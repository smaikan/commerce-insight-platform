"use server";

import { revalidatePath } from "next/cache";

import { ApiError } from "@/lib/api/problem";
import {
  cancelAccountOrder,
  createAccountAddress,
  deleteAccountAddress,
  setDefaultAccountAddress,
  updateAccountAddress,
  updateAccountUser,
} from "@/modules/account/api";
import type { AccountActionState, AddressPayload } from "@/modules/account/contracts";

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

// Burada adres formunu yalnız belgelenmiş alanlara indirip zorunlu alan hatalarını tek yerde üretiyorum.
function addressPayload(formData: FormData): { payload: AddressPayload | null; errors: Record<string, string> } {
  const values = {
    title: fieldValue(formData, "title"),
    firstName: fieldValue(formData, "firstName"),
    lastName: fieldValue(formData, "lastName"),
    phoneNumber: fieldValue(formData, "phoneNumber"),
    city: fieldValue(formData, "city"),
    district: fieldValue(formData, "district"),
    fullAddress: fieldValue(formData, "fullAddress"),
    postalCode: fieldValue(formData, "postalCode"),
  };
  const errors: Record<string, string> = {};
  for (const [key, value] of Object.entries(values)) {
    if (key !== "postalCode" && !value) errors[key] = "Bu alan zorunludur.";
  }

  const rawType = fieldValue(formData, "type");
  const type = rawType === "1" ? 1 : rawType === "0" ? 0 : null;
  if (type === null) errors.type = "Adres türünü seçin.";
  if (Object.keys(errors).length || type === null) return { payload: null, errors };

  return {
    payload: {
      type,
      ...values,
      postalCode: values.postalCode || null,
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
