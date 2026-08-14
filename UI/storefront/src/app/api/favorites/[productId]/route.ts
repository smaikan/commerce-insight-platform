import { hasTrustedStorefrontOrigin } from "@/lib/security/storefront-origin";
import { isProductPublicId } from "@/modules/favorites/request";
import { forwardFavoriteMutationRequest } from "@/modules/favorites/server/favorite-proxy";
import { favoriteProblemResponse } from "@/modules/favorites/server/route-response";

type FavoriteRouteContext = {
  params: Promise<{ productId: string }>;
};

// Burada route parametresindeki ürün kimliğini canonical public ID biçiminde çözüyorum.
async function favoriteProductId(context: FavoriteRouteContext): Promise<string | null> {
  const { productId } = await context.params;
  return isProductPublicId(productId) ? productId : null;
}

// Burada favoriye eklemeyi trusted browser origin kontrolünden sonra owner-aware BFF'ye iletiyorum.
export async function POST(request: Request, context: FavoriteRouteContext) {
  if (!hasTrustedStorefrontOrigin(request)) {
    return favoriteProblemResponse(403, "İstek reddedildi", "Favori isteğinin kaynağı doğrulanamadı.", "invalid_origin");
  }

  const productId = await favoriteProductId(context);
  if (!productId) {
    return favoriteProblemResponse(400, "Geçersiz ürün", "Ürün kimliği geçerli değil.", "validation_error");
  }

  return forwardFavoriteMutationRequest(request, productId, "POST");
}

// Burada favoriden çıkarmayı aynı origin ve ürün kimliği kontrolleriyle owner-aware BFF'ye iletiyorum.
export async function DELETE(request: Request, context: FavoriteRouteContext) {
  if (!hasTrustedStorefrontOrigin(request)) {
    return favoriteProblemResponse(403, "İstek reddedildi", "Favori isteğinin kaynağı doğrulanamadı.", "invalid_origin");
  }

  const productId = await favoriteProductId(context);
  if (!productId) {
    return favoriteProblemResponse(400, "Geçersiz ürün", "Ürün kimliği geçerli değil.", "validation_error");
  }

  return forwardFavoriteMutationRequest(request, productId, "DELETE");
}
