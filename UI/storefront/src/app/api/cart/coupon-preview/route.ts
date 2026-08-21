import { hasTrustedStorefrontOrigin, forwardCartRequest, problemResponse } from "@/modules/cart/server/cart-proxy";

export async function POST(request: Request) {
  if (!hasTrustedStorefrontOrigin(request)) {
    return problemResponse(403, "İstek reddedildi", "Kupon isteğinin kaynağı doğrulanamadı.", "invalid_origin");
  }

  const body = await request.text().catch(() => null);
  if (!body) {
    return problemResponse(400, "Geçersiz istek", "Kupon kodu sağlanmadı.", "validation_error");
  }

  return forwardCartRequest(request, "/api/cart/coupon-preview", {
    method: "POST",
    body,
  });
}
