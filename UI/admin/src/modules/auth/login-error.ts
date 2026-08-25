import { ApiError, retryAfterSeconds } from "../../lib/api/problem";
import type { LoginActionState } from "./types";

// Burada auth hatalarını hesap varlığını ifşa etmeyen ve parola içermeyen güvenli login durumlarına eşliyorum.
export function loginError(error: unknown, email: string): LoginActionState {
  if (!(error instanceof ApiError)) {
    return {
      status: "error",
      message: "Giriş şu anda tamamlanamadı. Lütfen tekrar deneyin.",
      email,
    };
  }

  if (error.problem.status === 401) {
    return { status: "error", message: "E-posta veya parola hatalı.", email };
  }
  if (error.problem.status === 403) {
    return { status: "error", message: "Bu panel yalnızca aktif yönetici hesaplarına açıktır.", email };
  }
  if (error.problem.status === 429) {
    const waitSeconds = retryAfterSeconds(error.problem.retryAfter);
    return {
      status: "error",
      message: waitSeconds
        ? `Giriş trafiği kısa süreli sınırlandı. Lütfen ${waitSeconds} saniye sonra tekrar deneyin.`
        : "Giriş trafiği kısa süreli sınırlandı. Lütfen biraz sonra tekrar deneyin.",
      email,
    };
  }

  return {
    status: "error",
    message: error.problem.status >= 500
      ? "Kimlik doğrulama servisine şu anda ulaşılamıyor. Lütfen tekrar deneyin."
      : "Giriş tamamlanamadı. Bilgilerinizi kontrol edin.",
    email,
    traceId: error.problem.traceId,
  };
}
