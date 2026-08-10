import { ApiError } from "../api/problem";
import type { AdminMutationResult } from "./mutation-result";

// Burada silme ve hızlı durum işlemlerindeki API hatalarını ortak kullanıcı mesajlarına dönüştürüyorum.
export function adminMutationError(error: unknown, fallback: string, conflictMessage: string): AdminMutationResult {
  if (!(error instanceof ApiError)) return { status: "error", message: fallback };
  const message = error.problem.status === 401
    ? "Oturumunuz sona erdi. Yeniden giriş yapıp işlemi tekrar deneyin."
    : error.problem.status === 403
      ? "Bu işlem için aktif yönetici yetkiniz bulunmuyor."
      : error.problem.status === 404
        ? "Kayıt artık bulunamıyor. Listeyi yenileyin."
        : error.problem.status === 409
          ? conflictMessage
          : error.problem.detail || fallback;
  return { status: "error", message, traceId: error.problem.traceId };
}
