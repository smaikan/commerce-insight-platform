import type { ForgotPasswordFieldErrors, ResetPasswordFieldErrors } from "@/modules/auth/password-reset-validation";

export type ForgotPasswordActionState = {
  status: "idle" | "success" | "error";
  revision: number;
  message?: string;
  fieldErrors?: ForgotPasswordFieldErrors;
  values?: { email?: string };
};

export type ResetPasswordActionState = {
  status: "idle" | "success" | "error" | "invalid-link";
  revision: number;
  message?: string;
  fieldErrors?: ResetPasswordFieldErrors;
};

// Burada parola bağlantısı formunun ilk durumunu Server Action dosyasından ayrı tutuyorum.
export const initialForgotPasswordState: ForgotPasswordActionState = { status: "idle", revision: 0 };

// Burada yeni parola formunun ilk durumunu token değerinden bağımsız ve güvenli biçimde tanımlıyorum.
export const initialResetPasswordState: ResetPasswordActionState = { status: "idle", revision: 0 };
