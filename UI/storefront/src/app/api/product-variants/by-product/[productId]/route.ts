import { forwardGuestCommerceRequest } from "@/modules/checkout/server/guest-commerce-proxy";

const PRODUCT_ID_PATTERN = /^P[0-9A-Z]+$/;

// Burada değişim ekranının public varyant sorgusunu yalnız canonical ürün kimliğiyle same-origin üzerinden iletiyorum.
export async function GET(request: Request, { params }: { params: Promise<{ productId: string }> }) {
  const { productId } = await params;
  if (!PRODUCT_ID_PATTERN.test(productId)) return Response.json({ status: 400, title: "Geçersiz ürün isteği" }, { status: 400 });
  return forwardGuestCommerceRequest(request, `/api/product-variants/by-product/${productId}?pageNumber=1&pageSize=100`, { method: "GET", cookieNames: [] });
}
