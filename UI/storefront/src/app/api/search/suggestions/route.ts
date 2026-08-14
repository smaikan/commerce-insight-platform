import { NextResponse } from "next/server";

import { getSearchSuggestions } from "@/modules/search/api";
import { isSearchQueryValid, normalizeSearchQuery } from "@/modules/search/query";
import { searchProblemResponse, searchRouteError } from "@/modules/search/server/route-response";

// Burada browser canlı aramasını zorunlu sorgu doğrulamasıyla server-only API katmanına bağlıyorum.
export async function GET(request: Request) {
  const query = normalizeSearchQuery(new URL(request.url).searchParams.get("q") || "");
  if (!isSearchQueryValid(query)) {
    return searchProblemResponse(
      400,
      "Geçersiz arama metni",
      "Arama metni 2 ile 100 karakter arasında olmalıdır.",
      "validation_error",
    );
  }

  try {
    const result = await getSearchSuggestions(query, request.signal);
    return NextResponse.json(result, { headers: { "Cache-Control": "private, no-store" } });
  } catch (error) {
    return searchRouteError(error);
  }
}
