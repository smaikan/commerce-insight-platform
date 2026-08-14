import "server-only";

import { NextResponse } from "next/server";

import { ApiError } from "@/lib/api/problem";

// Burada upstream ProblemDetails alanlarını ve varsa Retry-After bilgisini güvenli same-origin cevabına taşıyorum.
export function searchRouteError(error: unknown): NextResponse {
  if (error instanceof ApiError) {
    const headers = new Headers({
      "Cache-Control": "private, no-store",
      "Content-Type": "application/problem+json",
    });
    if (error.problem.retryAfter) headers.set("Retry-After", error.problem.retryAfter);
    return NextResponse.json(error.problem, { status: error.problem.status, headers });
  }

  return searchProblemResponse(
    503,
    "Arama şu anda kullanılamıyor",
    "Lütfen kısa bir süre sonra tekrar deneyin.",
    "search_unavailable",
  );
}

// Burada frontend doğrulama ve bağlantı hatalarını ortak, cache dışı ProblemDetails biçiminde üretiyorum.
export function searchProblemResponse(status: number, title: string, detail: string, code: string): NextResponse {
  return NextResponse.json(
    { status, title, detail, code },
    { status, headers: { "Cache-Control": "private, no-store", "Content-Type": "application/problem+json" } },
  );
}
