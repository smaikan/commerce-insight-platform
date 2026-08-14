"use server";

import { ApiError } from "@/lib/api/problem";
import { clearAuthCookies } from "@/lib/auth/cookies";
import { requestPasswordReset, resetCustomerPassword } from "@/modules/auth/api";
import type { ForgotPasswordActionState, ResetPasswordActionState } from "@/modules/auth/password-reset-state";
import {
  validateForgotPassword,
  validateResetPassword,
  type ForgotPasswordFieldErrors,
  type ResetPasswordFieldErrors,
} from "@/modules/auth/password-reset-validation";

const FORGOT_PASSWORD_SUCCESS = "Bu e-posta sistemde kayıtlıysa parola sıfırlama bağlantısı gönderildi.";
const INVALID_RESET_LINK = "Bu parola sıfırlama bağlantısı geçersiz, kullanılmış veya süresi dolmuş. Yeni bir bağlantı isteyin.";
const RATE_LIMIT_MESSAGE = "Çok fazla parola sıfırlama isteği yapıldı. Lütfen bir süre bekleyip yeniden deneyin.";

// Burada e-postayı doğrulayıp kullanıcı varlığını açıklamayan tek bir başarı sonucu döndürüyorum.
export async function forgotPasswordAction(
  state: ForgotPasswordActionState,
  formData: FormData,
): Promise<ForgotPasswordActionState> {
  const revision = state.revision + 1;
  const validation = validateForgotPassword(formData);
  if (!validation.success) {
    return {
      status: "error",
      revision,
      message: "Lütfen e-posta alanını kontrol edin.",
      fieldErrors: validation.errors,
      values: validation.values,
    };
  }

  try {
    await requestPasswordReset(validation.data);
  } catch (error) {
    return forgotPasswordErrorState(error, revision, validation.data.email);
  }

  return { status: "success", revision, message: FORGOT_PASSWORD_SUCCESS };
}

// Burada tokenı kullanıcıya veya hata durumuna taşımadan yeni parolayı API'ye gönderip başarılı işlemden sonra oturumu temizliyorum.
export async function resetPasswordAction(
  token: string,
  state: ResetPasswordActionState,
  formData: FormData,
): Promise<ResetPasswordActionState> {
  const revision = state.revision + 1;
  if (!token || !token.trim()) {
    return { status: "invalid-link", revision, message: INVALID_RESET_LINK };
  }

  const validation = validateResetPassword(formData);
  if (!validation.success) {
    return {
      status: "error",
      revision,
      message: "Lütfen işaretli alanları kontrol edin.",
      fieldErrors: validation.errors,
    };
  }

  try {
    await resetCustomerPassword({ token, newPassword: validation.data.newPassword });
  } catch (error) {
    return resetPasswordErrorState(error, revision);
  }

  await clearAuthCookies();
  return {
    status: "success",
    revision,
    message: "Parolanız değiştirildi. Yeni parolanızla giriş yapabilirsiniz.",
  };
}

// Burada forgot-password ProblemDetails kodlarını e-posta değerini koruyan güvenli form mesajlarına eşliyorum.
function forgotPasswordErrorState(error: unknown, revision: number, email: string): ForgotPasswordActionState {
  if (!(error instanceof ApiError)) {
    return { status: "error", revision, message: "Bağlantı kurulamadı. Lütfen biraz sonra tekrar deneyin.", values: { email } };
  }

  if (error.problem.code === "rate_limit_exceeded") {
    return { status: "error", revision, message: RATE_LIMIT_MESSAGE, values: { email } };
  }

  if (error.problem.code === "bad_request") {
    return {
      status: "error",
      revision,
      message: "Lütfen e-posta alanını kontrol edin.",
      fieldErrors: forgotPasswordApiErrors(error.problem.errors),
      values: { email },
    };
  }

  return { status: "error", revision, message: "Parola bağlantısı şu anda gönderilemedi. Lütfen biraz sonra tekrar deneyin.", values: { email } };
}

// Burada reset-password hata kodlarını token ayrıntısını açığa çıkarmayan form veya bağlantı durumlarına çeviriyorum.
function resetPasswordErrorState(error: unknown, revision: number): ResetPasswordActionState {
  if (!(error instanceof ApiError)) {
    return { status: "error", revision, message: "Bağlantı kurulamadı. Parolanız değiştirilmedi; lütfen yeniden deneyin." };
  }

  if (error.problem.code === "invalid_or_expired_reset_token" || error.problem.code === "concurrency_conflict") {
    return { status: "invalid-link", revision, message: INVALID_RESET_LINK };
  }

  if (error.problem.code === "rate_limit_exceeded") {
    return { status: "error", revision, message: RATE_LIMIT_MESSAGE };
  }

  if (error.problem.code === "bad_request") {
    const fieldErrors = resetPasswordApiErrors(error.problem.errors);
    if (fieldErrors) {
      return { status: "error", revision, message: "Lütfen işaretli alanları kontrol edin.", fieldErrors };
    }
    return { status: "invalid-link", revision, message: INVALID_RESET_LINK };
  }

  return { status: "error", revision, message: "Parolanız şu anda değiştirilemedi. Lütfen yeniden deneyin." };
}

// Burada API'nin e-posta alan hatasını yalnız ilgili Storefront alanına taşıyorum.
function forgotPasswordApiErrors(errors?: Record<string, string[]>): ForgotPasswordFieldErrors {
  const message = readApiFieldError(errors, "email");
  return { email: message || "Geçerli bir e-posta adresi girin." };
}

// Burada yalnız yeni parola alanına ait API doğrulama mesajını forma taşıyıp token hatasını kullanıcı alanı gibi göstermiyorum.
function resetPasswordApiErrors(errors?: Record<string, string[]>): ResetPasswordFieldErrors | undefined {
  const message = readApiFieldError(errors, "newPassword");
  return message ? { newPassword: message } : undefined;
}

// Burada API hata anahtarlarını büyük-küçük harften bağımsız biçimde güvenli bir metne indiriyorum.
function readApiFieldError(errors: Record<string, string[]> | undefined, fieldName: string): string | undefined {
  const entry = Object.entries(errors || {}).find(([key]) => key.toLowerCase() === fieldName.toLowerCase());
  return entry?.[1]?.[0];
}
