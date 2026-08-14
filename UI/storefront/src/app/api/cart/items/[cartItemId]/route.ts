import {
  isUuid,
  parseCartConcurrencyRequest,
  parseUpdateCartItemRequest,
} from "@/modules/cart/request";
import {
  forwardCartRequest,
  hasTrustedStorefrontOrigin,
  problemResponse,
} from "@/modules/cart/server/cart-proxy";

type CartItemRouteContext = {
  params: Promise<{ cartItemId: string }>;
};

async function validatedCartItemId(context: CartItemRouteContext): Promise<string | null> {
  const { cartItemId } = await context.params;
  return isUuid(cartItemId) ? cartItemId : null;
}

// Burada yalnızca doğrulanmış satır kimliği, adet ve concurrency token ile sepet satırını güncelliyorum.
export async function PUT(request: Request, context: CartItemRouteContext) {
  if (!hasTrustedStorefrontOrigin(request)) {
    return problemResponse(403, "İstek reddedildi", "Sepet isteğinin kaynağı doğrulanamadı.", "invalid_origin");
  }

  const cartItemId = await validatedCartItemId(context);
  const value = parseUpdateCartItemRequest(await request.json().catch(() => null));
  if (!cartItemId || !value) {
    return problemResponse(400, "Geçersiz sepet isteği", "Sepet satırı, adet ve sürüm bilgisi geçerli olmalıdır.", "validation_error");
  }

  return forwardCartRequest(request, `/api/cart/items/${cartItemId}`, {
    method: "PUT",
    body: JSON.stringify(value),
  });
}

// Burada satır silme isteğinde kimlik ile son sepet sürümünü ayrı ayrı doğruluyorum.
export async function DELETE(request: Request, context: CartItemRouteContext) {
  if (!hasTrustedStorefrontOrigin(request)) {
    return problemResponse(403, "İstek reddedildi", "Sepet isteğinin kaynağı doğrulanamadı.", "invalid_origin");
  }

  const cartItemId = await validatedCartItemId(context);
  const value = parseCartConcurrencyRequest(await request.json().catch(() => null));
  if (!cartItemId || !value) {
    return problemResponse(400, "Geçersiz sepet isteği", "Sepet satırı ve güncel sürüm bilgisi geçerli olmalıdır.", "validation_error");
  }

  return forwardCartRequest(
    request,
    `/api/cart/items/${cartItemId}?expectedConcurrencyToken=${encodeURIComponent(value.expectedConcurrencyToken)}`,
    { method: "DELETE" },
  );
}
