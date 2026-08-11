import "server-only";

import { siteConfig } from "@/lib/site-config";
import { ApiError, normalizeApiProblem } from "@/lib/api/problem";

type ApiRequestOptions = {
  revalidate?: number | false;
  tags?: string[];
  signal?: AbortSignal;
};

const DEFAULT_TIMEOUT_MS = 8_000;

// Burada yalnız uygulamanın iç API origin'i altında güvenli ve normalize edilmiş endpoint URL'si üretiyorum.
export function internalApiUrl(path: string): URL {
  const normalizedPath = path.startsWith("/") ? path : `/${path}`;
  return new URL(normalizedPath, `${siteConfig.apiUrl}/`);
}

// Burada public GET isteklerini timeout, açık cache kararı ve ProblemDetails ayrıştırmasıyla tek sınırdan geçiriyorum.
export async function apiGet<T>(path: string, options: ApiRequestOptions = {}): Promise<T> {
  const timeoutSignal = AbortSignal.timeout(DEFAULT_TIMEOUT_MS);
  const signal = options.signal ? AbortSignal.any([options.signal, timeoutSignal]) : timeoutSignal;
  const response = await fetch(internalApiUrl(path), {
    method: "GET",
    headers: { Accept: "application/json" },
    signal,
    next: {
      revalidate: options.revalidate ?? 60,
      tags: options.tags,
    },
  });

  if (!response.ok) {
    const contentType = response.headers.get("content-type") || "";
    const body = contentType.includes("json") ? await response.json().catch(() => null) : null;
    throw new ApiError(normalizeApiProblem(response.status, body));
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}
