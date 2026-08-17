import { isUuid } from "@/lib/validation/identifiers";
import { checkoutProblemResponse, forwardGuestCommerceRequest } from "@/modules/checkout/server/guest-commerce-proxy";

// Burada çapraz sipariş/talep kimliklerini API sahiplik denetimine bırakmadan önce biçimsel olarak daraltıyorum.
export async function GET(request: Request, { params }: { params: Promise<{ orderId: string; returnId: string }> }) {
  const { orderId, returnId } = await params;
  if (!isUuid(orderId) || !isUuid(returnId)) return checkoutProblemResponse(400, "Geçersiz talep isteği", "Talep kimlikleri geçerli değil.", "validation_error");
  return forwardGuestCommerceRequest(request, `/api/guest-orders/${orderId}/returns/${returnId}`, { method: "GET", cookieNames: ["ecommerce_guest_orders"] });
}
