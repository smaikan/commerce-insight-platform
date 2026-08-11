"use client";

import type {
  CheckoutProblem,
  GuestCheckoutRequest,
  GuestOrder,
} from "@/modules/checkout/types";

async function requestCheckout(path: string, init: RequestInit): Promise<GuestOrder> {
  const response = await fetch(path, {
    ...init,
    cache: "no-store",
    credentials: "same-origin",
  });
  const body = await response.json().catch(() => null);

  if (!response.ok) {
    const source = body && typeof body === "object" ? (body as Record<string, unknown>) : {};
    throw {
      status: response.status,
      title: typeof source.title === "string" ? source.title : "Sipariş isteği tamamlanamadı",
      detail: typeof source.detail === "string" ? source.detail : undefined,
      code: typeof source.code === "string" ? source.code : undefined,
      traceId: typeof source.traceId === "string" ? source.traceId : undefined,
      errors: isValidationErrors(source.errors) ? source.errors : undefined,
    } satisfies CheckoutProblem;
  }

  return body as GuestOrder;
}

// Burada aynı checkout intent'inin body ve idempotency anahtarını değiştirmeden same-origin BFF'e gönderiyorum.
export function submitGuestCheckout(
  value: GuestCheckoutRequest,
  idempotencyKey: string,
  turnstileToken?: string,
): Promise<GuestOrder> {
  return requestCheckout("/api/cart/checkout/guest", {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      "Idempotency-Key": idempotencyKey,
      ...(turnstileToken ? { "X-Turnstile-Token": turnstileToken } : {}),
    },
    body: JSON.stringify(value),
  });
}

// Burada confirmation sayfasında yalnız session grant'inin izin verdiği guest siparişi no-store olarak okuyorum.
export function loadGuestOrder(orderId: string): Promise<GuestOrder> {
  return requestCheckout(`/api/guest-orders/${encodeURIComponent(orderId)}`, { method: "GET" });
}

export function checkoutProblemMessage(error: unknown): string {
  if (!error || typeof error !== "object") return "İşlem tamamlanamadı. Lütfen tekrar deneyin.";

  const problem = error as Partial<CheckoutProblem>;
  if (problem.code === "coupon_members_only") return "Bu kupon yalnızca üyeler içindir. Kuponu kaldırıp tekrar deneyebilirsiniz.";
  if (problem.code === "guest_checkout_challenge_required") return "Devam etmek için güvenlik doğrulamasını tamamlayın.";
  if (problem.code === "guest_checkout_rate_limited") return "Çok fazla sipariş denemesi yapıldı. Lütfen belirtilen süre sonunda tekrar deneyin.";
  if (problem.code === "guest_checkout_protection_unavailable") return "Güvenlik doğrulama servisine ulaşılamıyor. Lütfen daha sonra tekrar deneyin.";
  if (problem.code === "idempotency_key_reused") return "Sipariş bilgileri önceki denemeden sonra değişti. Lütfen formu kontrol edip tekrar gönderin.";
  if (problem.status === 409) return "Sepet, stok, kargo veya kupon bilgisi değişti. Sepetin son halini kontrol edin.";
  if (problem.status === 404) return "Sepet veya seçilen kargo yöntemi artık kullanılamıyor.";
  if (problem.status === 400) return problem.detail || "Form alanlarını kontrol edin.";
  return problem.detail || "Sipariş şu anda tamamlanamıyor. Bilgileriniz korunarak tekrar deneyebilirsiniz.";
}

export function confirmationProblemMessage(error: unknown): string {
  if (error && typeof error === "object" && (error as Partial<CheckoutProblem>).status === 404) {
    return "Bu cihaz için geçerli sipariş erişimi bulunamadı veya süresi doldu.";
  }
  return checkoutProblemMessage(error);
}

export function checkoutTraceId(error: unknown): string | undefined {
  return error && typeof error === "object" ? (error as Partial<CheckoutProblem>).traceId : undefined;
}

export function checkoutFieldErrors(error: unknown): Record<string, string[]> | undefined {
  return error && typeof error === "object" ? (error as Partial<CheckoutProblem>).errors : undefined;
}

export function isCartConflict(error: unknown): boolean {
  if (!error || typeof error !== "object") return false;
  const problem = error as Partial<CheckoutProblem>;
  return problem.status === 409 && problem.code === "concurrency_conflict";
}

// Burada yalnız API'nin kesin 428 challenge kodunu Turnstile kurtarma akışına yönlendiriyorum.
export function isCheckoutChallengeRequired(error: unknown): boolean {
  if (!error || typeof error !== "object") return false;
  const problem = error as Partial<CheckoutProblem>;
  return problem.status === 428 && problem.code === "guest_checkout_challenge_required";
}

function isValidationErrors(value: unknown): value is Record<string, string[]> {
  return Boolean(
    value &&
      typeof value === "object" &&
      Object.values(value).every(
        (messages) => Array.isArray(messages) && messages.every((message) => typeof message === "string"),
      ),
  );
}
