import "server-only";

import { siteConfig } from "@/lib/site-config";
import { ApiError, normalizeApiProblem } from "@/lib/api/problem";

type ApiRequestOptions = {
  revalidate?: number | false;
  tags?: string[];
  signal?: AbortSignal;
  cache?: RequestCache;
};

type ApiPostOptions = {
  signal?: AbortSignal;
  headers?: HeadersInit;
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
  // Burada yüksek kardinaliteli canlı aramayı Next cache'ine sokmadan diğer public okumaların etiketli cache kararını koruyorum.
  const response = await fetch(internalApiUrl(path), {
    method: "GET",
    headers: { Accept: "application/json" },
    signal,
    ...(options.cache
      ? { cache: options.cache }
      : { next: { revalidate: options.revalidate ?? 60, tags: options.tags } }),
  });

  if (!response.ok) {
    const contentType = response.headers.get("content-type") || "";
    const body = contentType.includes("json") ? await response.json().catch(() => null) : null;
    throw new ApiError(normalizeApiProblem(response.status, body, response.headers.get("retry-after")));
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

// Burada public POST isteklerini otomatik tekrar yapmadan, boş başarı gövdelerini ve ProblemDetails hatalarını güvenli biçimde işliyorum.
export async function apiPost<T>(path: string, body?: unknown, options: ApiPostOptions = {}): Promise<T> {
  const headers = new Headers(options.headers);
  headers.set("Accept", "application/json");
  if (body !== undefined) headers.set("Content-Type", "application/json");

  const timeoutSignal = AbortSignal.timeout(DEFAULT_TIMEOUT_MS);
  const signal = options.signal ? AbortSignal.any([options.signal, timeoutSignal]) : timeoutSignal;
  const response = await fetch(internalApiUrl(path), {
    method: "POST",
    headers,
    body: body === undefined ? undefined : JSON.stringify(body),
    cache: "no-store",
    signal,
  });

  if (!response.ok) {
    const contentType = response.headers.get("content-type") || "";
    const responseBody = contentType.includes("json") ? await response.json().catch(() => null) : null;
    throw new ApiError(normalizeApiProblem(
      response.status,
      responseBody,
      response.headers.get("Retry-After") || undefined,
    ));
  }

  if (response.status === 202 || response.status === 204 || response.status === 205) {
    return undefined as T;
  }

  const contentLength = response.headers.get("content-length");
  if (contentLength === "0") return undefined as T;
  return await response.json() as T;
}
