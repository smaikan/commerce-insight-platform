import { isUuid } from "@/lib/validation/identifiers";
import {
  checkoutProblemResponse,
  forwardGuestCommerceRequest,
} from "@/modules/checkout/server/guest-commerce-proxy";

type GuestOrderRouteContext = {
  params: Promise<{ orderId: string }>;
};

// Burada yalnız geçerli order kimliği ve HttpOnly guest session cookie'siyle confirmation verisini okuyorum.
export async function GET(request: Request, context: GuestOrderRouteContext) {
  const { orderId } = await context.params;
  if (!isUuid(orderId)) {
    return checkoutProblemResponse(400, "Geçersiz sipariş isteği", "Sipariş kimliği geçerli değil.", "validation_error");
  }

  return forwardGuestCommerceRequest(request, `/api/guest-orders/${orderId}`, {
    method: "GET",
    cookieNames: ["ecommerce_guest_orders"],
  });
}
