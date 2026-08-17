import { forwardGuestCommerceRequest } from "@/modules/checkout/server/guest-commerce-proxy";

// Burada URL fragmentinden browser'da alınan tek kullanımlık erişim tokenını kalıcı olarak saklamadan guest session'a çeviriyorum.
export async function POST(request: Request) {
  return forwardGuestCommerceRequest(request, "/api/guest-orders/access/exchange", { method: "POST", body: await request.text(), cookieNames: [] });
}
