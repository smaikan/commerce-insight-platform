import { isUuid } from "@/lib/validation/identifiers";
import { checkoutProblemResponse } from "@/modules/checkout/server/guest-commerce-proxy";
import { forwardCheckoutOrderRead } from "@/modules/checkout/server/checkout-order-proxy";

// Burada sonuç ve confirmation ekranlarının order kimliğini biçimsel olarak doğrulayıp owner-aware okumaya iletiyorum.
export async function GET(request: Request, { params }: { params: Promise<{ orderId: string }> }) {
  const { orderId } = await params;
  if (!isUuid(orderId)) return checkoutProblemResponse(400, "Geçersiz sipariş isteği", "Sipariş kimliği geçerli değil.", "validation_error");
  return forwardCheckoutOrderRead(request, orderId);
}
