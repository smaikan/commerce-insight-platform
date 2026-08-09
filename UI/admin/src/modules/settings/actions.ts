"use server";

import { revalidatePath } from "next/cache";
import { redirect } from "next/navigation";
import { ApiError } from "@/lib/api/problem";
import { clearSessionCookies } from "@/lib/auth/cookies";
import { requireAdminActionSession } from "@/lib/auth/session";
import {
  changeAccountEmail,
  changeAccountPassword,
  createShippingMethod,
  createTaxRate,
  revokeAccountSession,
  revokeAllAccountSessions,
  setShippingMethodActivation,
  setTaxRateActivation,
  updateAccountProfile,
  updateShippingMethod,
  updateTaxRate,
} from "@/modules/settings/api";
import type {
  CreateShippingMethodRequest,
  CreateTaxRateRequest,
  SettingsActionState,
} from "@/modules/settings/types";

// Burada yeni kargo yöntemi formunu doğrulayıp gerçek yönetim endpoint'ine gönderiyorum.
export async function createShippingMethodAction(
  _previousState: SettingsActionState,
  formData: FormData,
): Promise<SettingsActionState> {
  const parsed = parseShippingMethodForm(formData);
  if (!parsed.ok) return parsed.state;
  const session = await actionSession();
  if (!session.ok) return session.state;

  try {
    await createShippingMethod(parsed.value, session.value);
  } catch (error) {
    return actionError(error, "Kargo yöntemi oluşturulamadı");
  }

  revalidatePath("/settings/shipping-methods");
  redirect("/settings/shipping-methods?created=1");
}

// Burada mevcut kargo yönteminin yalnızca güncellenebilir alanlarını kaydediyorum.
export async function updateShippingMethodAction(
  id: string,
  _previousState: SettingsActionState,
  formData: FormData,
): Promise<SettingsActionState> {
  const parsed = parseShippingMethodForm(formData);
  if (!parsed.ok) return parsed.state;
  const session = await actionSession();
  if (!session.ok) return session.state;

  try {
    const { name, fixedFee, displayOrder } = parsed.value;
    await updateShippingMethod(id, { name, fixedFee, displayOrder }, session.value);
  } catch (error) {
    return actionError(error, "Kargo yöntemi güncellenemedi");
  }

  revalidatePath("/settings/shipping-methods");
  redirect("/settings/shipping-methods?updated=1");
}

// Burada kargo yönteminin aktiflik durumunu bağımsız endpoint üzerinden değiştiriyorum.
export async function setShippingMethodActivationAction(
  id: string,
  isActive: boolean,
  previousState: SettingsActionState,
): Promise<SettingsActionState> {
  void previousState;
  const session = await actionSession();
  if (!session.ok) return session.state;

  try {
    await setShippingMethodActivation(id, isActive, session.value);
    revalidatePath("/settings/shipping-methods");
    return { status: "success", message: isActive ? "Kargo yöntemi etkinleştirildi." : "Kargo yöntemi pasifleştirildi." };
  } catch (error) {
    return actionError(error, "Kargo yöntemi durumu değiştirilemedi");
  }
}

// Burada yeni vergi oranı formunu doğrulayıp gerçek yönetim endpoint'ine gönderiyorum.
export async function createTaxRateAction(
  _previousState: SettingsActionState,
  formData: FormData,
): Promise<SettingsActionState> {
  const parsed = parseTaxRateForm(formData);
  if (!parsed.ok) return parsed.state;
  const session = await actionSession();
  if (!session.ok) return session.state;

  try {
    await createTaxRate(parsed.value, session.value);
  } catch (error) {
    return actionError(error, "Vergi oranı oluşturulamadı");
  }

  revalidatePath("/settings/tax-rates");
  redirect("/settings/tax-rates?created=1");
}

// Burada mevcut vergi oranının yalnızca güncellenebilir alanlarını kaydediyorum.
export async function updateTaxRateAction(
  id: string,
  _previousState: SettingsActionState,
  formData: FormData,
): Promise<SettingsActionState> {
  const parsed = parseTaxRateForm(formData);
  if (!parsed.ok) return parsed.state;
  const session = await actionSession();
  if (!session.ok) return session.state;

  try {
    const { name, rate } = parsed.value;
    await updateTaxRate(id, { name, rate }, session.value);
  } catch (error) {
    return actionError(error, "Vergi oranı güncellenemedi");
  }

  revalidatePath("/settings/tax-rates");
  redirect("/settings/tax-rates?updated=1");
}

// Burada vergi oranının aktiflik durumunu bağımsız endpoint üzerinden değiştiriyorum.
export async function setTaxRateActivationAction(
  id: string,
  isActive: boolean,
  previousState: SettingsActionState,
): Promise<SettingsActionState> {
  void previousState;
  const session = await actionSession();
  if (!session.ok) return session.state;

  try {
    await setTaxRateActivation(id, isActive, session.value);
    revalidatePath("/settings/tax-rates");
    return { status: "success", message: isActive ? "Vergi oranı etkinleştirildi." : "Vergi oranı pasifleştirildi." };
  } catch (error) {
    return actionError(error, "Vergi oranı durumu değiştirilemedi");
  }
}

// Burada profil formunun güvenli alanlarını doğrulayıp oturum sahibine kaydediyorum.
export async function updateProfileAction(
  _previousState: SettingsActionState,
  formData: FormData,
): Promise<SettingsActionState> {
  const firstName = textValue(formData, "firstName");
  const lastName = textValue(formData, "lastName");
  const phoneNumber = textValue(formData, "phoneNumber");
  const fieldErrors: Record<string, string[]> = {};
  if (!firstName) fieldErrors.firstName = ["Ad zorunludur."];
  if (firstName.length > 100) fieldErrors.firstName = ["Ad en fazla 100 karakter olabilir."];
  if (!lastName) fieldErrors.lastName = ["Soyad zorunludur."];
  if (lastName.length > 100) fieldErrors.lastName = ["Soyad en fazla 100 karakter olabilir."];
  if (phoneNumber.length > 30) fieldErrors.phoneNumber = ["Telefon en fazla 30 karakter olabilir."];
  if (Object.keys(fieldErrors).length) return validationState(fieldErrors);

  const session = await actionSession();
  if (!session.ok) return session.state;
  try {
    await updateAccountProfile({ firstName, lastName, phoneNumber: phoneNumber || null }, session.value);
    revalidatePath("/settings/account");
    revalidatePath("/", "layout");
    return { status: "success", message: "Profil bilgileriniz güncellendi." };
  } catch (error) {
    return actionError(error, "Profil güncellenemedi");
  }
}

// Burada e-posta değişikliğini mevcut parola ve yeni adres doğrulamasıyla kaydediyorum.
export async function changeEmailAction(
  _previousState: SettingsActionState,
  formData: FormData,
): Promise<SettingsActionState> {
  const currentPassword = String(formData.get("currentPassword") ?? "");
  const newEmail = textValue(formData, "newEmail");
  const fieldErrors: Record<string, string[]> = {};
  if (!currentPassword) fieldErrors.currentPassword = ["Mevcut parola zorunludur."];
  if (currentPassword.length > 128) fieldErrors.currentPassword = ["Parola en fazla 128 karakter olabilir."];
  if (!isEmail(newEmail)) fieldErrors.newEmail = ["Geçerli bir e-posta adresi girin."];
  if (newEmail.length > 320) fieldErrors.newEmail = ["E-posta en fazla 320 karakter olabilir."];
  if (Object.keys(fieldErrors).length) return validationState(fieldErrors);

  const session = await actionSession();
  if (!session.ok) return session.state;
  try {
    await changeAccountEmail({ currentPassword, newEmail }, session.value);
    revalidatePath("/settings/account");
    revalidatePath("/", "layout");
    return { status: "success", message: "E-posta adresiniz güncellendi." };
  } catch (error) {
    return actionError(error, "E-posta değiştirilemedi");
  }
}

// Burada parola değişikliğini sözleşmedeki uzunluk ve farklılık kurallarıyla doğruluyorum.
export async function changePasswordAction(
  _previousState: SettingsActionState,
  formData: FormData,
): Promise<SettingsActionState> {
  const currentPassword = String(formData.get("currentPassword") ?? "");
  const newPassword = String(formData.get("newPassword") ?? "");
  const confirmPassword = String(formData.get("confirmPassword") ?? "");
  const fieldErrors: Record<string, string[]> = {};
  if (!currentPassword) fieldErrors.currentPassword = ["Mevcut parola zorunludur."];
  if (currentPassword.length > 128) fieldErrors.currentPassword = ["Parola en fazla 128 karakter olabilir."];
  if (newPassword.length < 6 || newPassword.length > 128) fieldErrors.newPassword = ["Yeni parola 6–128 karakter olmalıdır."];
  if (newPassword === currentPassword) fieldErrors.newPassword = ["Yeni parola mevcut paroladan farklı olmalıdır."];
  if (confirmPassword !== newPassword) fieldErrors.confirmPassword = ["Parola tekrarı yeni parolayla eşleşmiyor."];
  if (Object.keys(fieldErrors).length) return validationState(fieldErrors);

  const session = await actionSession();
  if (!session.ok) return session.state;
  try {
    await changeAccountPassword({ currentPassword, newPassword }, session.value);
    return { status: "success", message: "Parolanız güncellendi." };
  } catch (error) {
    return actionError(error, "Parola değiştirilemedi");
  }
}

// Burada seçilen cihaz oturumunu sonlandırıp güvenlik listesini yetkili kaynaktan yeniliyorum.
export async function revokeSessionAction(
  sessionId: string,
  previousState: SettingsActionState,
): Promise<SettingsActionState> {
  void previousState;
  const session = await actionSession();
  if (!session.ok) return session.state;

  try {
    await revokeAccountSession(sessionId, session.value);
    revalidatePath("/settings/security");
    return { status: "success", message: "Oturum sonlandırıldı." };
  } catch (error) {
    return actionError(error, "Oturum sonlandırılamadı");
  }
}

// Burada tüm oturumları backend'de geçersiz kılıp yerel kimlik doğrulama cookie'lerini temizliyorum.
export async function revokeAllSessionsAction(
  previousState: SettingsActionState,
): Promise<SettingsActionState> {
  void previousState;
  const session = await actionSession();
  if (!session.ok) return session.state;

  try {
    await revokeAllAccountSessions(session.value);
    await clearSessionCookies();
  } catch (error) {
    return actionError(error, "Oturumlar sonlandırılamadı");
  }

  redirect("/login?reason=sessions_revoked");
}

type ShippingFormResult =
  | { ok: true; value: CreateShippingMethodRequest }
  | { ok: false; state: SettingsActionState };

// Burada kargo formundaki para ve sıralama değerlerini API sınırlarıyla doğruluyorum.
function parseShippingMethodForm(formData: FormData): ShippingFormResult {
  const name = textValue(formData, "name");
  const fixedFee = decimalValue(formData, "fixedFee");
  const displayOrder = integerValue(formData, "displayOrder");
  const fieldErrors: Record<string, string[]> = {};
  if (!name || name.length > 150) fieldErrors.name = ["Kargo yöntemi adı 1–150 karakter olmalıdır."];
  if (fixedFee === null || fixedFee < 0) fieldErrors.fixedFee = ["Ücret sıfır veya daha büyük olmalıdır."];
  if (displayOrder === null || displayOrder < 0) fieldErrors.displayOrder = ["Sıralama sıfır veya daha büyük tam sayı olmalıdır."];
  if (Object.keys(fieldErrors).length) return { ok: false, state: validationState(fieldErrors) };

  return { ok: true, value: { name, fixedFee: fixedFee!, displayOrder: displayOrder!, isActive: formData.get("isActive") === "on" } };
}

type TaxFormResult =
  | { ok: true; value: CreateTaxRateRequest }
  | { ok: false; state: SettingsActionState };

// Burada vergi formundaki yüzde değerini backend'in 0–100 aralığıyla doğruluyorum.
function parseTaxRateForm(formData: FormData): TaxFormResult {
  const name = textValue(formData, "name");
  const rate = decimalValue(formData, "rate");
  const fieldErrors: Record<string, string[]> = {};
  if (!name || name.length > 100) fieldErrors.name = ["Vergi adı 1–100 karakter olmalıdır."];
  if (rate === null || rate < 0 || rate > 100) fieldErrors.rate = ["Vergi oranı 0–100 arasında olmalıdır."];
  if (Object.keys(fieldErrors).length) return { ok: false, state: validationState(fieldErrors) };

  return { ok: true, value: { name, rate: rate!, isActive: formData.get("isActive") === "on" } };
}

// Burada her mutation öncesinde aktif Admin oturumunu yeniden doğruluyorum.
async function actionSession() {
  try {
    return { ok: true as const, value: await requireAdminActionSession() };
  } catch (error) {
    return { ok: false as const, state: actionError(error, "Yönetici oturumu doğrulanamadı") };
  }
}

// Burada ProblemDetails hatasını formun koruyabileceği güvenli bir sonuca dönüştürüyorum.
function actionError(error: unknown, prefix: string): SettingsActionState {
  if (error instanceof ApiError) {
    const message = error.problem.status === 403
      ? "Bu işlem yalnızca aktif yönetici hesaplarına açıktır."
      : error.problem.status === 401
        ? "Oturumunuz sona erdi. Form veriniz korunuyor; yeniden giriş yapın."
        : `${prefix}: ${error.problem.detail || error.problem.title}`;
    return { status: "error", message, traceId: error.problem.traceId, fieldErrors: error.problem.errors };
  }
  return { status: "error", message: `${prefix}. Lütfen tekrar deneyin.` };
}

// Burada yerel alan hatalarını ortak form durumuna taşıyorum.
function validationState(fieldErrors: Record<string, string[]>): SettingsActionState {
  return { status: "error", message: "İşaretli alanları kontrol edin.", fieldErrors };
}

// Burada FormData metinlerini kırpılmış güvenli değerlere dönüştürüyorum.
function textValue(formData: FormData, name: string): string {
  return String(formData.get(name) ?? "").trim();
}

// Burada ondalık form değerlerini hem nokta hem virgül ayracıyla okuyorum.
function decimalValue(formData: FormData, name: string): number | null {
  const raw = textValue(formData, name).replace(",", ".");
  const value = Number(raw);
  return raw && Number.isFinite(value) ? value : null;
}

// Burada sıralama alanının gerçekten tam sayı olduğunu doğruluyorum.
function integerValue(formData: FormData, name: string): number | null {
  const raw = textValue(formData, name);
  const value = Number(raw);
  return raw && Number.isInteger(value) ? value : null;
}

// Burada e-posta alanına temel istemci tarafı biçim kontrolü uyguluyorum; son otorite backend'dir.
function isEmail(value: string): boolean {
  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value);
}
