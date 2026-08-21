import { hasTrustedStorefrontOrigin } from "@/lib/security/storefront-origin";
import {
  contactProblemResponse,
  forwardContactSubmission,
} from "@/modules/contact/server/contact-proxy";
import {
  isSafeIdempotencyKey,
  parseContactSubmission,
  parseContactTurnstileToken,
} from "@/modules/contact/validation";

const CONTACT_REQUEST_LIMIT_BYTES = 16 * 1024;

// Burada browser-facing contact mutation'ını origin, boyut, generated body ve güvenlik header sınırlarından geçiriyorum.
export async function POST(request: Request) {
  if (!hasTrustedStorefrontOrigin(request)) {
    return contactProblemResponse(
      403,
      "İstek reddedildi",
      "İletişim isteğinin kaynağı doğrulanamadı. Sayfayı yenileyip tekrar deneyin.",
      "invalid_origin",
    );
  }

  const contentLength = Number(request.headers.get("content-length") || 0);
  if (Number.isFinite(contentLength) && contentLength > CONTACT_REQUEST_LIMIT_BYTES) {
    return contactProblemResponse(413, "İstek çok büyük", "Mesaj içeriğini kısaltıp tekrar deneyin.", "payload_too_large");
  }

  const rawBody = await request.text();
  if (new TextEncoder().encode(rawBody).byteLength > CONTACT_REQUEST_LIMIT_BYTES) {
    return contactProblemResponse(413, "İstek çok büyük", "Mesaj içeriğini kısaltıp tekrar deneyin.", "payload_too_large");
  }

  let parsedBody: unknown;
  try {
    parsedBody = JSON.parse(rawBody || "null") as unknown;
  } catch {
    return contactProblemResponse(400, "Geçersiz istek", "İletişim formu geçerli JSON içermiyor.", "bad_request");
  }

  const idempotencyKey = request.headers.get("idempotency-key")?.trim() || null;
  const turnstileToken = parseContactTurnstileToken(request.headers.get("x-turnstile-token"));
  const value = parseContactSubmission(parsedBody);

  if (!isSafeIdempotencyKey(idempotencyKey) || turnstileToken === null) {
    return contactProblemResponse(
      400,
      "Geçersiz iletişim isteği",
      "İstek güvenlik bilgileri geçersiz. Sayfayı yenileyip tekrar deneyin.",
      "validation_error",
    );
  }
  if (!value.ok) {
    return contactProblemResponse(
      400,
      "Form alanlarını kontrol edin",
      value.formError || "İletişim formundaki işaretli alanları düzeltin.",
      "validation_error",
      value.errors,
    );
  }

  return forwardContactSubmission(value.value, idempotencyKey, turnstileToken);
}
