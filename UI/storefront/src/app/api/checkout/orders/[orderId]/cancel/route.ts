import { hasTrustedStorefrontOrigin } from "@/lib/security/storefront-origin";
import { isUuid } from "@/lib/validation/identifiers";
import { checkoutProblemResponse } from "@/modules/checkout/server/guest-commerce-proxy";
import { forwardCheckoutOrderCancellation } from "@/modules/checkout/server/checkout-order-proxy";

// Burada browser iptal isteğini origin ve order kimliğiyle doğrulayıp owner-aware iptal proxy'sine aktarıyorum.
export async function POST(request: Request, { params }: { params: Promise<{ orderId: string }> }) {
  const { orderId } = await params;
  if (!hasTrustedStorefrontOrigin(request)) return checkoutProblemResponse(403, "İstek reddedildi", "İptal isteğinin kaynağı doğrulanamadı.", "invalid_origin");
  if (!isUuid(orderId)) return checkoutProblemResponse(400, "Geçersiz iptal isteği", "Sipariş kimliği geçerli değil.", "validation_error");
  return forwardCheckoutOrderCancellation(request, orderId);
}
