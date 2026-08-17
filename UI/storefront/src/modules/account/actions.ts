"use server";

import { revalidatePath } from "next/cache";

import { clearAuthCookies } from "@/lib/auth/cookies";
import { ApiError } from "@/lib/api/problem";
import {
  cancelAccountOrder,
  createAccountReturn,
  changeAccountPassword,
  createAccountAddress,
  deleteAccountAddress,
  logoutAllAccountSessions,
  revokeAccountSession,
  setDefaultAccountAddress,
  updateAccountAddress,
  updateAccountUser,
} from "@/modules/account/api";
import type { AccountActionState, AddressPayload } from "@/modules/account/contracts";
import { redirect } from "next/navigation";

const UUID_PATTERN = /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;

// Burada profil formundaki düzenlenebilir alanları API isteğine dönüştürmeden önce doğruluyorum.
export async function updateProfileAction(
  state: AccountActionState,
  formData: FormData,
): Promise<AccountActionState> {
  const revision = state.revision + 1;
  const firstName = fieldValue(formData, "firstName");
  const lastName = fieldValue(formData, "lastName");
  const phoneNumber = fieldValue(formData, "phoneNumber");
  const fieldErrors: Record<string, string> = {};

  if (!firstName) fieldErrors.firstName = "Ad alanı zorunludur.";
  if (!lastName) fieldErrors.lastName = "Soyad alanı zorunludur.";
  if (Object.keys(fieldErrors).length) return invalidState(revision, fieldErrors);

  try {
    await updateAccountUser({ firstName, lastName, phoneNumber: phoneNumber || null });
    revalidatePath("/account");
    return { status: "success", revision, message: "Profil bilgileriniz güncellendi." };
  } catch (error) {
    return accountErrorState(error, revision, "Profil bilgileriniz güncellenemedi.");
  }
}

// Burada aynı erişilebilir formu hem yeni adres hem de mevcut adres düzenlemesi için güvenli payload'a çeviriyorum.
export async function saveAddressAction(
  addressId: string | null,
  state: AccountActionState,
  formData: FormData,
): Promise<AccountActionState> {
  const revision = state.revision + 1;
  if (addressId && !UUID_PATTERN.test(addressId)) {
    return { status: "error", revision, message: "Adres kaydı geçersiz." };
  }

  const parsed = addressPayload(formData);
  if (!parsed.payload) return invalidState(revision, parsed.errors);

  try {
    if (addressId) await updateAccountAddress(addressId, parsed.payload);
    else await createAccountAddress(parsed.payload);
    revalidatePath("/account");
    revalidatePath("/account/addresses");
    return {
      status: "success",
      revision,
      message: addressId ? "Adresiniz güncellendi." : "Yeni adresiniz kaydedildi.",
    };
  } catch (error) {
    return accountErrorState(error, revision, "Adres kaydedilemedi.");
  }
}

// Burada varsayılan adres değişikliğini yalnız geçerli adres kimliğiyle çalıştırıyorum.
export async function setDefaultAddressAction(id: string): Promise<AccountActionState> {
  if (!UUID_PATTERN.test(id)) return { status: "error", revision: 1, message: "Adres kaydı geçersiz." };
  try {
    await setDefaultAccountAddress(id);
    revalidatePath("/account");
    revalidatePath("/account/addresses");
    return { status: "success", revision: 1, message: "Varsayılan adres güncellendi." };
  } catch (error) {
    return accountErrorState(error, 1, "Varsayılan adres değiştirilemedi.");
  }
}

// Burada silme onayından gelen adres kimliğini doğrulayıp yalnız API sahiplik sınırında kaldırıyorum.
export async function deleteAddressAction(id: string): Promise<AccountActionState> {
  if (!UUID_PATTERN.test(id)) return { status: "error", revision: 1, message: "Adres kaydı geçersiz." };
  try {
    await deleteAccountAddress(id);
    revalidatePath("/account");
    revalidatePath("/account/addresses");
    return { status: "success", revision: 1, message: "Adres silindi." };
  } catch (error) {
    return accountErrorState(error, 1, "Adres silinemedi.");
  }
}

// Burada müşteri iptalini yalnız geçerli sipariş kimliğiyle API'nin yaşam döngüsü kurallarına iletiyorum.
export async function cancelOrderAction(id: string): Promise<AccountActionState> {
  if (!UUID_PATTERN.test(id)) return { status: "error", revision: 1, message: "Sipariş kaydı geçersiz." };
  try {
    await cancelAccountOrder(id);
    revalidatePath("/account");
    revalidatePath("/account/orders");
    revalidatePath(`/account/orders/${id}`);
    return { status: "success", revision: 1, message: "Siparişiniz iptal edildi." };
  } catch (error) {
    return accountErrorState(error, 1, "Sipariş iptal edilemedi.");
  }
}

// Burada iade formundaki seçili kalemleri doğrulayıp API'nin tek authoritative talep komutuna dönüştürüyorum.
export async function createReturnAction(
  orderId: string,
  state: AccountActionState,
  formData: FormData,
): Promise<AccountActionState> {
  const revision = state.revision + 1;
  if (!UUID_PATTERN.test(orderId)) return { status: "error", revision, message: "Sipariş kaydı geçersiz." };

  const typeValue = fieldValue(formData, "type");
  const type = typeValue === "0" ? 0 : typeValue === "1" ? 1 : null;
  const itemIds = formData.getAll("orderItemId").filter((value): value is string => typeof value === "string" && UUID_PATTERN.test(value));
  const items = itemIds.flatMap((orderItemId) => {
    const quantity = Number(fieldValue(formData, `quantity:${orderItemId}`));
    if (!Number.isInteger(quantity) || quantity <= 0) return [];
    const replacement = fieldValue(formData, `replacement:${orderItemId}`);
    return [{
      orderItemId,
      quantity,
      replacementProductVariantId: type === 1 && UUID_PATTERN.test(replacement) ? replacement : null,
    }];
  });

  if (type === null) return invalidState(revision, { type: "Talep türünü seçin." });
  if (!items.length) return { status: "error", revision, message: "En az bir ürün için iade adedi seçin." };
  if (type === 1 && items.some((item) => !item.replacementProductVariantId)) {
    return { status: "error", revision, message: "Değişime eklenen her ürün için yeni varyant seçin." };
  }

  let created;
  try {
    created = await createAccountReturn({
      orderId,
      type,
      items,
      customerNote: fieldValue(formData, "customerNote") || null,
    });
  } catch (error) {
    return accountErrorState(error, revision, "İade veya değişim talebiniz oluşturulamadı.");
  }

  revalidatePath("/account/returns");
  revalidatePath(`/account/orders/${orderId}`);
  redirect(`/account/returns/${created.id}`);
}

// Burada parola formunun üç alanını eşleştirip API doğrulamasından önce anlaşılır alan hataları üretiyorum.
export async function changePasswordAction(
  state: AccountActionState,
  formData: FormData,
): Promise<AccountActionState> {
  const revision = state.revision + 1;
  const currentPassword = fieldValue(formData, "currentPassword");
  const newPassword = fieldValue(formData, "newPassword");
  const confirmPassword = fieldValue(formData, "confirmPassword");
  const fieldErrors: Record<string, string> = {};

  if (!currentPassword) fieldErrors.currentPassword = "Mevcut parolanızı girin.";
  if (!newPassword) fieldErrors.newPassword = "Yeni parolanızı girin.";
  if (newPassword && newPassword === currentPassword) fieldErrors.newPassword = "Yeni parola mevcut paroladan farklı olmalıdır.";
  if (newPassword !== confirmPassword) fieldErrors.confirmPassword = "Yeni parola ve tekrarı eşleşmiyor.";
  if (Object.keys(fieldErrors).length) return invalidState(revision, fieldErrors);

  try {
    await changeAccountPassword({ currentPassword, newPassword });
    await clearAuthCookies();
    return { status: "success", revision, message: "Parolanız değiştirildi. Güvenliğiniz için yeniden giriş yapın." };
  } catch (error) {
    return accountErrorState(error, revision, "Parolanız değiştirilemedi.");
  }
}

// Burada oturum listesinde seçilen kimliği doğrulayıp yalnız o oturumu kapatıyorum.
export async function revokeSessionAction(id: string): Promise<AccountActionState> {
  if (!UUID_PATTERN.test(id)) return { status: "error", revision: 1, message: "Oturum kaydı geçersiz." };
  try {
    await revokeAccountSession(id);
    revalidatePath("/account/security");
    return { status: "success", revision: 1, message: "Seçili oturum kapatıldı." };
  } catch (error) {
    return accountErrorState(error, 1, "Oturum kapatılamadı.");
  }
}

// Burada API tarafında tüm oturumlar kapatıldıktan sonra yerel HttpOnly çerezleri de temizliyorum.
export async function logoutAllSessionsAction(): Promise<AccountActionState> {
  try {
    await logoutAllAccountSessions();
    await clearAuthCookies();
    return { status: "success", revision: 1, message: "Tüm cihazlardaki oturumlar kapatıldı." };
  } catch (error) {
    return accountErrorState(error, 1, "Oturumlar kapatılamadı.");
  }
}

// Burada adres formunu yalnız belgelenmiş alanlara indirip zorunlu alan hatalarını tek yerde üretiyorum.
function addressPayload(formData: FormData): { payload: AddressPayload | null; errors: Record<string, string> } {
  const values = {
    title: fieldValue(formData, "title"),
    firstName: fieldValue(formData, "firstName"),
    lastName: fieldValue(formData, "lastName"),
    phoneNumber: fieldValue(formData, "phoneNumber"),
    city: fieldValue(formData, "City"),
    district: fieldValue(formData, "District"),
    fullAddress: fieldValue(formData, "fullAddress"),
  };
  const postalCode = fieldValue(formData, "postalCode");
  const neighborhood = fieldValue(formData, "Neighborhood");

  const errors: Record<string, string> = {};
  for (const [key, value] of Object.entries(values)) {
    if (!value) errors[key] = "Bu alan zorunludur.";
  }

  const rawType = fieldValue(formData, "type");
  const type = rawType === "1" ? 1 : rawType === "0" ? 0 : null;
  if (type === null) errors.type = "Adres türünü seçin.";
  if (Object.keys(errors).length || type === null) return { payload: null, errors };

  return {
    payload: {
      type,
      ...values,
      ...(neighborhood ? { neighborhood } : {}),
      postalCode: postalCode || null,
      isDefault: formData.get("isDefault") === "on",
    },
    errors,
  };
}

// Burada FormData alanlarını güvenli biçimde metne normalleştiriyorum.
function fieldValue(formData: FormData, name: string): string {
  const value = formData.get(name);
  return typeof value === "string" ? value.trim() : "";
}

function invalidState(revision: number, fieldErrors: Record<string, string>): AccountActionState {
  return { status: "error", revision, message: "Lütfen işaretli alanları kontrol edin.", fieldErrors };
}

// Burada API ProblemDetails alanlarını formda güvenli ve anlaşılır bir sonuca dönüştürüyorum.
function accountErrorState(error: unknown, revision: number, fallback: string): AccountActionState {
  if (!(error instanceof ApiError)) {
    return { status: "error", revision, message: "Bağlantı kurulamadı. Lütfen tekrar deneyin." };
  }
  if (error.problem.status === 401) {
    return { status: "error", revision, message: "Oturumunuz sona erdi. Sayfayı yenileyip tekrar giriş yapın." };
  }
  if (error.problem.status === 409) {
    return { status: "error", revision, message: "Kayıt bu sırada değişti. Sayfayı yenileyip tekrar deneyin." };
  }
  if (error.problem.status === 400 && error.problem.errors) {
    const fieldErrors = Object.fromEntries(
      Object.entries(error.problem.errors).flatMap(([key, messages]) => messages[0] ? [[lowerFirst(key), messages[0]]] : []),
    );
    return { status: "error", revision, message: "Lütfen işaretli alanları kontrol edin.", fieldErrors };
  }
  return { status: "error", revision, message: fallback };
}

function lowerFirst(value: string): string {
  return value ? value[0].toLowerCase() + value.slice(1) : value;
}
