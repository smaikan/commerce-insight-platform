import { parseCartConcurrencyRequest } from "@/modules/cart/request";
import {
  forwardCartRequest,
  hasTrustedStorefrontOrigin,
  problemResponse,
} from "@/modules/cart/server/cart-proxy";

// Burada browser'ın kullanıcı veya misafir sepetini hassas sahiplik bilgisini görmeden okumasını sağlıyorum.
export async function GET(request: Request) {
  return forwardCartRequest(request, "/api/cart", { method: "GET" });
}

// Burada sepeti temizleme isteğini aynı-origin ve güncel concurrency token doğrulamasından sonra iletiyorum.
export async function DELETE(request: Request) {
  if (!hasTrustedStorefrontOrigin(request)) {
    return problemResponse(403, "İstek reddedildi", "Sepet isteğinin kaynağı doğrulanamadı.", "invalid_origin");
  }

  const value = parseCartConcurrencyRequest(await request.json().catch(() => null));
  if (!value) {
    return problemResponse(400, "Geçersiz sepet isteği", "Güncel sepet sürümü bulunamadı.", "validation_error");
  }

  return forwardCartRequest(
    request,
    `/api/cart?expectedConcurrencyToken=${encodeURIComponent(value.expectedConcurrencyToken)}`,
    { method: "DELETE" },
  );
}
