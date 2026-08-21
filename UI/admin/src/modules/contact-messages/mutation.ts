import { ApiError } from "../../lib/api/problem";
import type {
  ContactMessageActionResult,
  ContactMessageDetail,
  ContactMessageMutationSnapshot,
} from "./types";

// Burada tam ve PII içeren detail yerine Client Component'e yalnız mutasyon eşzamanlılık görünümünü taşıyorum.
export function contactMessageSnapshot(detail: ContactMessageDetail): ContactMessageMutationSnapshot {
  return {
    concurrencyToken: detail.concurrencyToken,
    status: detail.status,
    assignedAdminUserId: detail.assignedAdminUserId,
    updatedAt: detail.updatedAt,
  };
}

// Burada ProblemDetails durumunu ve code değerini birbirinden ayırarak güvenli kullanıcı sonucuna dönüştürüyorum.
export function contactMessageMutationError(error: unknown, fallback: string): ContactMessageActionResult {
  if (!(error instanceof ApiError)) return { status: "error", message: fallback };
  const problem = error.problem;
  if (problem.status === 401) {
    return { status: "error", code: problem.code, message: "Oturumunuz sona erdi. Yeniden giriş yapıp işlemi tekrar deneyin.", traceId: problem.traceId };
  }
  if (problem.status === 403) {
    return { status: "error", code: problem.code, message: "Bu işlem için aktif yönetici yetkiniz bulunmuyor.", traceId: problem.traceId };
  }
  if (problem.status === 404) {
    return { status: "error", code: problem.code, message: "İletişim mesajı artık bulunamıyor. Gelen kutusuna dönün.", traceId: problem.traceId };
  }
  if (problem.status === 409 && problem.code === "idempotency_key_reused") {
    return { status: "error", code: problem.code, message: "Bu gönderim anahtarı farklı bir yanıt için kullanılmış. Metni kontrol edip yeni bir gönderim başlatın.", traceId: problem.traceId };
  }
  if (problem.status === 429) {
    const wait = problem.retryAfter ? ` ${problem.retryAfter} sonra tekrar deneyin.` : " Bir süre sonra tekrar deneyin.";
    return { status: "error", code: problem.code, message: `Çok fazla istek gönderildi.${wait}`, traceId: problem.traceId, retryAfter: problem.retryAfter };
  }
  if (problem.status === 400) {
    return {
      status: "error",
      code: problem.code,
      message: problem.detail || "Girilen alanları kontrol edin.",
      traceId: problem.traceId,
      fieldErrors: normalizeContactFieldErrors(problem.errors),
    };
  }
  return {
    status: "error",
    code: problem.code,
    message: problem.detail || fallback,
    traceId: problem.traceId,
    fieldErrors: normalizeContactFieldErrors(problem.errors),
  };
}

// Burada ASP.NET PascalCase alan anahtarlarını kalıcı HTML kontrol adlarıyla camelCase eşliyorum.
export function normalizeContactFieldErrors(errors?: Record<string, string[]>): Record<string, string[]> | undefined {
  if (!errors) return undefined;
  return Object.fromEntries(Object.entries(errors).map(([key, messages]) => [key.charAt(0).toLocaleLowerCase("tr-TR") + key.slice(1), messages]));
}

// Burada aynı reply intentinin retry boyunca koruduğu opaque idempotency anahtarını üretiyorum.
export function createContactReplyIdempotencyKey(randomUuid: () => string = () => crypto.randomUUID()): string {
  return `CONTACT_REPLY_${randomUuid().replaceAll("-", "").toUpperCase()}`;
}

export type ContactReplyIntent = { key: string; attemptedBody?: string };

// Burada network retry'da key'i koruyup, gönderilmiş intent metni değiştiğinde yeni key üretirim.
export function contactReplyIntentAfterEdit(
  intent: ContactReplyIntent,
  nextBody: string,
  randomUuid: () => string = () => crypto.randomUUID(),
): ContactReplyIntent {
  return intent.attemptedBody !== undefined && nextBody.trim() !== intent.attemptedBody
    ? { key: createContactReplyIdempotencyKey(randomUuid) }
    : intent;
}

// Burada concurrency kabulünde kullanıcının metin taslağını aynen koruyan yeni mutation bağlamını üretirim.
export function preserveContactDraftOnConflict<T>(draft: T, snapshot: ContactMessageMutationSnapshot): { draft: T; snapshot: ContactMessageMutationSnapshot } {
  return { draft, snapshot };
}
