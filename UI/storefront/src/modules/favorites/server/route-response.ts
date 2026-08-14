import "server-only";

import { NextResponse } from "next/server";

import { ApiError } from "@/lib/api/problem";

// Burada upstream favori hatalarını hassas sunucu ayrıntıları olmadan same-origin ProblemDetails cevabına çeviriyorum.
export function favoriteRouteError(error: unknown): NextResponse {
  if (error instanceof ApiError) {
    const headers = new Headers({
      "Cache-Control": "private, no-store",
      "Content-Type": "application/problem+json",
    });
    if (error.problem.retryAfter) headers.set("Retry-After", error.problem.retryAfter);
    return NextResponse.json(error.problem, { status: error.problem.status, headers });
  }

  return favoriteProblemResponse(
    503,
    "Favoriler şu anda kullanılamıyor",
    "Lütfen kısa bir süre sonra tekrar deneyin.",
    "favorites_unavailable",
  );
}

// Burada frontend doğrulama ve origin hatalarını tutarlı, cache dışı ProblemDetails biçiminde üretiyorum.
export function favoriteProblemResponse(status: number, title: string, detail: string, code: string): NextResponse {
  return NextResponse.json(
    { status, title, detail, code },
    { status, headers: { "Cache-Control": "private, no-store", "Content-Type": "application/problem+json" } },
  );
}
