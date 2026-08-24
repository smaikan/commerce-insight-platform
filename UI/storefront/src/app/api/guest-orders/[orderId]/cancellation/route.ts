import { isUuid } from "@/lib/validation/identifiers";
import { checkoutProblemResponse, forwardGuestCommerceRequest } from "@/modules/checkout/server/guest-commerce-proxy";

// Burada magic-link guest polling isteğini yalnız HttpOnly order grant'inin erişebildiği operasyona iletiyorum.
export async function GET(request: Request, { params }: { params: Promise<{ orderId: string }> }) {
  const { orderId } = await params;
  if (!isUuid(orderId)) return checkoutProblemResponse(400, "Geçersiz iptal sorgusu", "Sipariş kimliği geçerli değil.", "validation_error");
  return forwardGuestCommerceRequest(request, `/api/guest-orders/${orderId}/cancellation`, {
    method: "GET",
    cookieNames: ["ecommerce_guest_orders"],
  });
}
