import { forwardGuestCommerceRequest } from "@/modules/checkout/server/guest-commerce-proxy";

// Burada hesap varlığını açıklamayan access-link isteğini public guest endpointine same-origin sınırından iletiyorum.
export async function POST(request: Request) {
  return forwardGuestCommerceRequest(request, "/api/guest-orders/access-links", { method: "POST", body: await request.text(), cookieNames: [] });
}
