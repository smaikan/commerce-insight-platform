import { parseAddCartItemRequest } from "@/modules/cart/request";
import {
  forwardCartRequest,
  hasTrustedStorefrontOrigin,
  problemResponse,
} from "@/modules/cart/server/cart-proxy";

// Burada sepete ekleme mutation'ını origin ve dar request gövdesi doğrulamasından sonra upstream API'ye iletiyorum.
export async function POST(request: Request) {
  if (!hasTrustedStorefrontOrigin(request)) {
    return problemResponse(403, "İstek reddedildi", "Sepet isteğinin kaynağı doğrulanamadı.", "invalid_origin");
  }

  const value = parseAddCartItemRequest(await request.json().catch(() => null));
  if (!value) {
    return problemResponse(400, "Geçersiz sepet isteği", "Ürün seçeneği ve adet bilgisi geçerli olmalıdır.", "validation_error");
  }

  return forwardCartRequest(request, "/api/cart/items", {
    method: "POST",
    body: JSON.stringify(value),
  });
}
