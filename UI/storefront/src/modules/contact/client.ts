import { ApiError, normalizeApiProblem } from "@/lib/api/problem";
import type { ContactSubmissionReceipt, ContactSubmissionRequest } from "@/modules/contact/types";

const CONTACT_TIMEOUT_MS = 20_000;

function isReceipt(value: unknown): value is ContactSubmissionReceipt {
  if (!value || typeof value !== "object") return false;
  const source = value as Record<string, unknown>;
  return typeof source.referenceNumber === "string" && source.referenceNumber.startsWith("CNT-") &&
    typeof source.submittedAt === "string" && !Number.isNaN(Date.parse(source.submittedAt));
}

// Burada browser'ın yalnız same-origin contact BFF sınırına typed, timeout'lu ve idempotent intent isteği göndermesini sağlıyorum.
export async function submitContactMessage(
  request: ContactSubmissionRequest,
  idempotencyKey: string,
  turnstileToken?: string,
): Promise<ContactSubmissionReceipt> {
  const response = await fetch("/api/contact-messages", {
    method: "POST",
    headers: {
      Accept: "application/json",
      "Content-Type": "application/json",
      "Idempotency-Key": idempotencyKey,
      ...(turnstileToken ? { "X-Turnstile-Token": turnstileToken } : {}),
    },
    body: JSON.stringify(request),
    cache: "no-store",
    signal: AbortSignal.timeout(CONTACT_TIMEOUT_MS),
  });

  const contentType = response.headers.get("content-type") || "";
  const body = contentType.includes("json") ? await response.json().catch(() => null) : null;
  if (!response.ok) {
    throw new ApiError(normalizeApiProblem(response.status, body, response.headers.get("Retry-After")));
  }

  if (response.status !== 202 || !isReceipt(body)) {
    throw new ApiError({
      title: "İletişim yanıtı doğrulanamadı",
      detail: "Talebinizin sonucu doğrulanamadı. Aynı form içeriğiyle tekrar deneyebilirsiniz.",
      status: 502,
      code: "contact_invalid_response",
    });
  }

  return body;
}
