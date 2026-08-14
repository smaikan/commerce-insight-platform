import type { ForgotPasswordPayload } from "@/modules/auth/contracts";

export type ForgotPasswordFieldErrors = Partial<Record<"email", string>>;
export type ResetPasswordFieldErrors = Partial<Record<"newPassword" | "confirmPassword", string>>;

type ForgotPasswordValidation =
  | { success: true; data: ForgotPasswordPayload }
  | { success: false; errors: ForgotPasswordFieldErrors; values: { email: string } };

type ResetPasswordValidation =
  | { success: true; data: { newPassword: string } }
  | { success: false; errors: ResetPasswordFieldErrors };

const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

// Burada parola bağlantısı isteyen e-postayı API'nin uzunluk ve biçim sınırlarına göre doğruluyorum.
export function validateForgotPassword(formData: FormData): ForgotPasswordValidation {
  const email = readText(formData, "email").trim().toLowerCase();
  const errors: ForgotPasswordFieldErrors = {};

  if (!email) errors.email = "E-posta adresinizi girin.";
  else if (email.length > 320 || !EMAIL_PATTERN.test(email)) errors.email = "Geçerli bir e-posta adresi girin.";

  return errors.email
    ? { success: false, errors, values: { email } }
    : { success: true, data: { email } };
}

// Burada yeni parola ile tekrarını eşleştirip API'nin 6–128 karakter sınırını istekten önce uyguluyorum.
export function validateResetPassword(formData: FormData): ResetPasswordValidation {
  const newPassword = readText(formData, "newPassword");
  const confirmPassword = readText(formData, "confirmPassword");
  const errors: ResetPasswordFieldErrors = {};

  if (!newPassword) errors.newPassword = "Yeni parolanızı girin.";
  else if (newPassword.length < 6) errors.newPassword = "Parola en az 6 karakter olmalı.";
  else if (newPassword.length > 128) errors.newPassword = "Parola en fazla 128 karakter olabilir.";

  if (!confirmPassword) errors.confirmPassword = "Yeni parolanızı tekrar girin.";
  else if (newPassword !== confirmPassword) errors.confirmPassword = "Parolalar birbiriyle eşleşmiyor.";

  return Object.keys(errors).length
    ? { success: false, errors }
    : { success: true, data: { newPassword } };
}

// Burada FormData içinden yalnız metin değerlerini okuyarak beklenmeyen dosya girdilerini yok sayıyorum.
function readText(formData: FormData, name: string): string {
  const value = formData.get(name);
  return typeof value === "string" ? value : "";
}
