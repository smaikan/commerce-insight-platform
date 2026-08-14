import { parseFavoritePage, parseFavoritePageSize } from "@/modules/favorites/request";
import { forwardFavoriteProductsRequest } from "@/modules/favorites/server/favorite-proxy";

// Burada favorites sayfasının guest ve authenticated ürün listesini doğrulanmış sayfalama ile BFF üzerinden getiriyorum.
export async function GET(request: Request) {
  const url = new URL(request.url);
  const pageNumber = parseFavoritePage(url.searchParams.get("pageNumber") || undefined);
  const pageSize = parseFavoritePageSize(url.searchParams.get("pageSize") || undefined);
  return forwardFavoriteProductsRequest(request, pageNumber, pageSize);
}
