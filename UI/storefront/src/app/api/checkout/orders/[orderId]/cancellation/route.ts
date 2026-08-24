import { isUuid } from "@/lib/validation/identifiers";
import { checkoutProblemResponse } from "@/modules/checkout/server/guest-commerce-proxy";
import { forwardCheckoutOrderCancellationRead } from "@/modules/checkout/server/checkout-order-proxy";

// Burada polling isteğini geçerli order kimliğiyle owner-aware ve private/no-store API sınırına iletiyorum.
export async function GET(request: Request, { params }: { params: Promise<{ orderId: string }> }) {
  const { orderId } = await params;
  if (!isUuid(orderId)) return checkoutProblemResponse(400, "Geçersiz iptal sorgusu", "Sipariş kimliği geçerli değil.", "validation_error");
  return forwardCheckoutOrderCancellationRead(request, orderId);
}
