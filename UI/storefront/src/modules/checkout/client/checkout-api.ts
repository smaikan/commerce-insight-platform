"use client";

import type {
  CheckoutProblem,
  CheckoutFormSession,
  CheckoutOrder,
  GuestCheckoutRequest,
  GuestOrder,
} from "@/modules/checkout/types";

let checkoutRefreshStarted = false;

async function requestCheckout<T>(path: string, init: RequestInit): Promise<T> {
  const response = await fetch(path, {
    ...init,
    cache: "no-store",
    credentials: "same-origin",
  });
  const body = await response.json().catch(() => null);

  if (!response.ok) {
    const source = body && typeof body === "object" ? (body as Record<string, unknown>) : {};
    const problem = {
      status: response.status,
      title: typeof source.title === "string" ? source.title : "Sipariş isteği tamamlanamadı",
      detail: typeof source.detail === "string" ? source.detail : undefined,
      code: typeof source.code === "string" ? source.code : undefined,
      traceId: typeof source.traceId === "string" ? source.traceId : undefined,
      errors: isValidationErrors(source.errors) ? source.errors : undefined,
    } satisfies CheckoutProblem;
    if (problem.code === "session_refresh_required") refreshCheckoutSession();
    throw problem;
  }

  return body as T;
}

// Burada süresi dolmuş üye access token'ında ödeme mutation'ını körlemesine tekrarlamadan refresh rotasına güvenli dönüş yapıyorum.
function refreshCheckoutSession(): void {
  if (checkoutRefreshStarted || typeof window === "undefined") return;
  checkoutRefreshStarted = true;
  const returnTo = `${window.location.pathname}${window.location.search}`;
  window.location.assign(`/api/auth/refresh?returnTo=${encodeURIComponent(returnTo)}`);
}

const pendingPaymentInitializations = new Map<string, Promise<CheckoutFormSession>>();
const ACTIVE_CHECKOUT_ORDER_KEY = "checkout:active-order";

// Burada aynı checkout intent'inin body ve idempotency anahtarını değiştirmeden same-origin BFF'e gönderiyorum.
export function submitGuestCheckout(
  value: GuestCheckoutRequest,
  idempotencyKey: string,
  turnstileToken?: string,
): Promise<GuestOrder> {
  return requestCheckout<GuestOrder>("/api/cart/checkout/guest", {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      "Idempotency-Key": idempotencyKey,
      ...(turnstileToken ? { "X-Turnstile-Token": turnstileToken } : {}),
    },
    body: JSON.stringify(value),
  });
}

// Burada girilen kupon kodunu ödeme öncesi doğrulayıp önizleme indirimi alıyorum.
export function previewCoupon(couponCode: string): Promise<{ code: string; discountTotal: number; discountType: number }> {
  return requestCheckout<{ code: string; discountTotal: number; discountType: number }>("/api/cart/coupon-preview", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ couponCode }),
  });
}

// Burada confirmation sayfasında yalnız session grant'inin izin verdiği guest siparişi no-store olarak okuyorum.
export function loadCheckoutOrder(orderId: string): Promise<CheckoutOrder> {
  return requestCheckout<CheckoutOrder>(`/api/checkout/orders/${encodeURIComponent(orderId)}`, { method: "GET" });
}

// Burada magic-link ile açılan confirmation ekranını olası üye oturumundan bağımsız olarak guest grant endpointinden okuyorum.
export function loadGuestCheckoutOrder(orderId: string): Promise<CheckoutOrder> {
  return requestCheckout<CheckoutOrder>(`/api/guest-orders/${encodeURIComponent(orderId)}`, { method: "GET" });
}

// Burada ödeme sağlayıcısına yönlenmeden önce yalnız yetki sağlamayan order kimliğini geri dönüş kurtarması için saklıyorum.
export function rememberActiveCheckoutOrder(
  orderId: string,
  storage: Pick<Storage, "setItem"> = window.localStorage,
): void {
  storage.setItem(ACTIVE_CHECKOUT_ORDER_KEY, orderId);
}

// Burada checkout'a geri dönüldüğünde yinelenen sipariş açmamak için bekleyen order kimliğini okuyorum.
export function activeCheckoutOrderId(
  storage: Pick<Storage, "getItem" | "removeItem"> = window.localStorage,
): string | null {
  const orderId = storage.getItem(ACTIVE_CHECKOUT_ORDER_KEY);
  if (!orderId || !/^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i.test(orderId)) {
    storage.removeItem(ACTIVE_CHECKOUT_ORDER_KEY);
    return null;
  }

  return orderId;
}

// Burada yalnız tamamlanan veya kullanıcıca iptal edilen order işaretini temizleyip aynı order için ödeme intent anahtarını da kaldırıyorum.
export function forgetActiveCheckoutOrder(
  orderId?: string,
  storage: Pick<Storage, "getItem" | "removeItem"> = window.localStorage,
): void {
  const activeOrderId = storage.getItem(ACTIVE_CHECKOUT_ORDER_KEY);
  if (!orderId || activeOrderId === orderId) storage.removeItem(ACTIVE_CHECKOUT_ORDER_KEY);
  if (orderId) storage.removeItem(`checkout:iyzico:${orderId}`);
}

// Burada hosted ödeme oturumunu kart verisi göndermeden yalnız stable idempotency anahtarıyla başlatıyorum.
export function initializeIyzicoCheckoutForm(orderId: string, idempotencyKey: string): Promise<CheckoutFormSession> {
  const intent = `${orderId}:${idempotencyKey}`;
  const pending = pendingPaymentInitializations.get(intent);
  if (pending) return pending;

  // Burada hızlı çift tıklamaların aynı ödeme intent'i için ikinci bir ağ isteği üretmesini promise düzeyinde engelliyorum.
  const request = requestCheckout<CheckoutFormSession>(`/api/checkout/orders/${encodeURIComponent(orderId)}/payments/iyzico/checkout-form`, {
    method: "POST",
    headers: { "Idempotency-Key": idempotencyKey },
  }).finally(() => pendingPaymentInitializations.delete(intent));
  pendingPaymentInitializations.set(intent, request);
  return request;
}

// Burada aynı order ödeme niyetinin retry'larında anahtarı kalıcı tutup yalnız açıkça yeni denemede yeniliyorum.
export function paymentIntentKey(orderId: string, newAttempt = false, storage: Pick<Storage, "getItem" | "setItem" | "removeItem"> = window.localStorage): string {
  const storageKey = `checkout:iyzico:${orderId}`;
  if (newAttempt) storage.removeItem(storageKey);
  const existing = storage.getItem(storageKey);
  if (existing) return existing;
  const created = crypto.randomUUID();
  storage.setItem(storageKey, created);
  return created;
}

// Burada API'nin nullable yönlendirme alanını mutlak HTTPS adresi olmadan browser navigasyonuna açmıyorum.
export function redirectToPaymentPage(session: CheckoutFormSession, assign: (url: string) => void = (url) => window.location.assign(url)): void {
  if (!session.paymentPageUrl) throw { status: 502, title: "Ödeme sayfası oluşturulamadı", code: "payment_page_missing" } satisfies CheckoutProblem;
  let target: URL;
  try { target = new URL(session.paymentPageUrl); } catch { throw { status: 502, title: "Ödeme sayfası oluşturulamadı", code: "payment_page_invalid" } satisfies CheckoutProblem; }
  if (target.protocol !== "https:") throw { status: 502, title: "Ödeme sayfası oluşturulamadı", code: "payment_page_invalid" } satisfies CheckoutProblem;
  assign(target.toString());
}

export function checkoutProblemMessage(error: unknown): string {
  if (!error || typeof error !== "object") return "İşlem tamamlanamadı. Lütfen tekrar deneyin.";

  const problem = error as Partial<CheckoutProblem>;
  if (problem.code === "coupon_members_only") return "Bu kupon yalnızca üyeler içindir. Kuponu kaldırıp tekrar deneyebilirsiniz.";
  if (problem.code === "guest_checkout_challenge_required") return "Devam etmek için güvenlik doğrulamasını tamamlayın.";
  if (problem.code === "guest_checkout_rate_limited") return "Çok fazla sipariş denemesi yapıldı. Lütfen belirtilen süre sonunda tekrar deneyin.";
  if (problem.code === "guest_checkout_protection_unavailable") return "Güvenlik doğrulama servisine ulaşılamıyor. Lütfen daha sonra tekrar deneyin.";
  if (problem.code === "idempotency_key_reused") return "Sipariş bilgileri önceki denemeden sonra değişti. Lütfen formu kontrol edip tekrar gönderin.";
  if (problem.status === 409) {
    if (problem.detail === "Coupon was not found." || problem.detail === "Coupon cannot be applied to this order.") {
      return "Girilen kupon yanlış, süresi dolmuş veya bu sipariş için geçerli değil.";
    }
    return "Sepet, stok, kargo veya kupon bilgisi değişti. Sepetin son halini kontrol edin.";
  }
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

export function paymentProblemMessage(error: unknown): string {
  if (error && typeof error === "object" && (error as Partial<CheckoutProblem>).status === 409) {
    return "Bu sipariş için devam eden bir ödeme bulunuyor. Birkaç saniye sonra sipariş durumunu yeniden kontrol edin.";
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
