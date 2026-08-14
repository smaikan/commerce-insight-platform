"use client";

import type {
  SearchClientProblem,
  SearchInspiration,
  SearchSuggestions,
} from "@/modules/search/types";

const SEARCH_SUGGESTIONS_PATH = "/api/search/suggestions";
const SEARCH_INSPIRATION_PATH = "/api/search/inspiration";

// Burada canlı suggestion isteğini same-origin BFF sınırına cache ve otomatik retry olmadan gönderiyorum.
export function requestSearchSuggestions(query: string, signal: AbortSignal): Promise<SearchSuggestions> {
  return requestSearchJson<SearchSuggestions>(
    `${SEARCH_SUGGESTIONS_PATH}?q=${encodeURIComponent(query)}`,
    signal,
    "no-store",
  );
}

// Burada modal açılışındaki ilham ürünlerini yalnız ihtiyaç anında ve tarayıcı cache'ine izin vererek yüklüyorum.
export function requestSearchInspiration(signal: AbortSignal): Promise<SearchInspiration> {
  return requestSearchJson<SearchInspiration>(SEARCH_INSPIRATION_PATH, signal, "default");
}

// Burada same-origin arama cevaplarını güvenli typed sonuca veya kullanıcıya gösterilebilir ProblemDetails hatasına ayırıyorum.
async function requestSearchJson<T>(path: string, signal: AbortSignal, cache: RequestCache): Promise<T> {
  const response = await fetch(path, {
    method: "GET",
    headers: { Accept: "application/json" },
    credentials: "same-origin",
    cache,
    signal,
  });
  const body = await response.json().catch(() => null);

  if (!response.ok) {
    const source = body && typeof body === "object" ? body as Record<string, unknown> : {};
    throw {
      status: response.status,
      title: typeof source.title === "string" ? source.title : "Arama tamamlanamadı",
      detail: typeof source.detail === "string" ? source.detail : undefined,
      code: typeof source.code === "string" ? source.code : undefined,
      traceId: typeof source.traceId === "string" ? source.traceId : undefined,
      retryAfter: response.headers.get("retry-after") || undefined,
    } satisfies SearchClientProblem;
  }

  return body as T;
}

// Burada bilinmeyen istemci hatasını 400, 429 ve genel bağlantı durumları için kararlı bir mesaja çeviriyorum.
export function searchErrorMessage(error: unknown): string {
  if (!error || typeof error !== "object") return "Arama şu anda tamamlanamıyor. Lütfen tekrar deneyin.";
  const problem = error as Partial<SearchClientProblem>;
  if (problem.status === 429) return "Çok fazla arama yapıldı. Lütfen kısa bir süre bekleyip tekrar deneyin.";
  if (problem.status === 400) return problem.detail || "Arama metnini kontrol edip tekrar deneyin.";
  return problem.detail || "Arama şu anda tamamlanamıyor. Bağlantınızı kontrol edip tekrar deneyin.";
}

export function isSearchRateLimited(error: unknown): boolean {
  return Boolean(error && typeof error === "object" && (error as Partial<SearchClientProblem>).status === 429);
}
