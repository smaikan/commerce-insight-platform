import { isUuid } from "@/lib/validation/identifiers";
import { checkoutProblemResponse, forwardGuestCommerceRequest } from "@/modules/checkout/server/guest-commerce-proxy";

type Context = { params: Promise<{ orderId: string }> };

// Burada misafir talep listesini yalnız order session cookie'sinin yetkilendirdiği sipariş için iletiyorum.
export async function GET(request: Request, context: Context) {
  const { orderId } = await context.params;
  if (!isUuid(orderId)) return checkoutProblemResponse(400, "Geçersiz sipariş isteği", "Sipariş kimliği geçerli değil.", "validation_error");
  const query = new URL(request.url).search;
  return forwardGuestCommerceRequest(request, `/api/guest-orders/${orderId}/returns${query}`, { method: "GET", cookieNames: ["ecommerce_guest_orders"] });
}

// Burada misafir iade mutasyonuna session ve CSRF cookie'lerini sunucu tarafında doğrulanmış header ile ekliyorum.
export async function POST(request: Request, context: Context) {
  const { orderId } = await context.params;
  if (!isUuid(orderId)) return checkoutProblemResponse(400, "Geçersiz sipariş isteği", "Sipariş kimliği geçerli değil.", "validation_error");
  return forwardGuestCommerceRequest(request, `/api/guest-orders/${orderId}/returns`, {
    method: "POST",
    body: await request.text(),
    cookieNames: ["ecommerce_guest_orders", "ecommerce_guest_csrf"],
    csrf: true,
  });
}
