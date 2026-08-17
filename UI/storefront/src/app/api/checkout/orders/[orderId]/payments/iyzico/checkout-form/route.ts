import { hasTrustedStorefrontOrigin } from "@/lib/security/storefront-origin";
import { isUuid } from "@/lib/validation/identifiers";
import { parseIdempotencyKey } from "@/modules/checkout/request";
import { checkoutProblemResponse } from "@/modules/checkout/server/guest-commerce-proxy";
import { forwardIyzicoCheckoutForm } from "@/modules/checkout/server/checkout-order-proxy";

// Burada browser ödeme niyetini origin, order kimliği ve dar idempotency sözleşmesiyle doğruladıktan sonra owner-aware proxy'ye aktarıyorum.
export async function POST(request: Request, { params }: { params: Promise<{ orderId: string }> }) {
  const { orderId } = await params;
  if (!hasTrustedStorefrontOrigin(request)) return checkoutProblemResponse(403, "İstek reddedildi", "Ödeme isteğinin kaynağı doğrulanamadı.", "invalid_origin");
  if (!isUuid(orderId)) return checkoutProblemResponse(400, "Geçersiz ödeme isteği", "Sipariş kimliği geçerli değil.", "validation_error");
  const idempotencyKey = parseIdempotencyKey(request.headers.get("idempotency-key"));
  if (!idempotencyKey) return checkoutProblemResponse(400, "Geçersiz ödeme isteği", "Ödeme denemesi anahtarı geçerli değil.", "validation_error");
  return forwardIyzicoCheckoutForm(request, orderId, idempotencyKey);
}
