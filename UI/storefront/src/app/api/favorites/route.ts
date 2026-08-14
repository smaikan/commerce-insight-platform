import { forwardFavoriteStateRequest } from "@/modules/favorites/server/favorite-proxy";

// Burada guest veya authenticated owner'ın favori kimliklerini tokenı browser'a açmadan BFF üzerinden topluyorum.
export async function GET(request: Request) {
  return forwardFavoriteStateRequest(request);
}
