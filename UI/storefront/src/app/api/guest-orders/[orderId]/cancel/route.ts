import { hasTrustedStorefrontOrigin } from "@/lib/security/storefront-origin";
import { isUuid } from "@/lib/validation/identifiers";
import { checkoutProblemResponse, forwardGuestCommerceRequest } from "@/modules/checkout/server/guest-commerce-proxy";

// Burada magic-link guest iptalini olası üye oturumundan bağımsız session, Origin ve CSRF sınırında çalıştırıyorum.
export async function POST(request: Request, { params }: { params: Promise<{ orderId: string }> }) {
  const { orderId } = await params;
  if (!hasTrustedStorefrontOrigin(request)) return checkoutProblemResponse(403, "İstek reddedildi", "İptal isteğinin kaynağı doğrulanamadı.", "invalid_origin");
  if (!isUuid(orderId)) return checkoutProblemResponse(400, "Geçersiz iptal isteği", "Sipariş kimliği geçerli değil.", "validation_error");
  return forwardGuestCommerceRequest(request, `/api/guest-orders/${orderId}/cancel`, {
    method: "POST",
    cookieNames: ["ecommerce_guest_orders", "ecommerce_guest_csrf"],
    csrf: true,
  });
}
