import { NextResponse } from "next/server";

import { getSearchInspiration } from "@/modules/search/api";
import { searchRouteError } from "@/modules/search/server/route-response";

// Burada modal açılışındaki küçük ilham vitrini için tek, kısa süreli public cache'lenebilir cevap sunuyorum.
export async function GET(request: Request) {
  try {
    const result = await getSearchInspiration(request.signal);
    return NextResponse.json(result, {
      headers: { "Cache-Control": "public, max-age=60, stale-while-revalidate=300" },
    });
  } catch (error) {
    return searchRouteError(error);
  }
}
