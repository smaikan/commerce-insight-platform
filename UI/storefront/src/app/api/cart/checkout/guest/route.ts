import { hasTrustedStorefrontOrigin } from "@/lib/security/storefront-origin";
import {
  parseGuestCheckoutRequest,
  parseIdempotencyKey,
  parseTurnstileToken,
} from "@/modules/checkout/request";
import {
  checkoutProblemResponse,
  forwardGuestCommerceRequest,
} from "@/modules/checkout/server/guest-commerce-proxy";
import { isCheckoutOrderCreationEnabled } from "@/modules/checkout/config";

// Burada guest checkout mutation'ını origin, idempotency, challenge ve dar gövde doğrulamasından sonra upstream'e iletiyorum.
export async function POST(request: Request) {
  if (!isCheckoutOrderCreationEnabled()) {
    return checkoutProblemResponse(503, "Online sipariş geçici olarak kapalı", "Ödeme seçeneği etkinleştirildiğinde siparişinizi tamamlayabilirsiniz.", "checkout_unavailable");
  }

  if (!hasTrustedStorefrontOrigin(request)) {
    return checkoutProblemResponse(403, "İstek reddedildi", "Sipariş isteğinin kaynağı doğrulanamadı.", "invalid_origin");
  }

  const idempotencyKey = parseIdempotencyKey(request.headers.get("idempotency-key"));
  const turnstileToken = parseTurnstileToken(request.headers.get("x-turnstile-token"));
  const value = parseGuestCheckoutRequest(await request.json().catch(() => null));
  if (!idempotencyKey || turnstileToken === null || !value) {
    return checkoutProblemResponse(400, "Geçersiz sipariş isteği", "İletişim, adres, kargo ve sepet bilgilerini kontrol edin.", "validation_error");
  }

  return forwardGuestCommerceRequest(request, "/api/cart/checkout/guest", {
    method: "POST",
    body: JSON.stringify(value),
    cookieNames: ["ecommerce_guest_cart", "ecommerce_guest_orders"],
    idempotencyKey,
    turnstileToken,
  });
}
