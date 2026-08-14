import "server-only";

import { internalApiUrl } from "@/lib/api/client";
import { ApiError, normalizeApiProblem } from "@/lib/api/problem";
import { readAccessToken } from "@/lib/auth/cookies";

type AuthenticatedRequestOptions = {
  method?: "GET" | "POST" | "PUT" | "PATCH" | "DELETE";
  body?: unknown;
  signal?: AbortSignal;
};

const ACCOUNT_REQUEST_TIMEOUT_MS = 8_000;

// Burada müşteri verisi isteklerini Bearer token, no-store ve ortak ProblemDetails davranışıyla sunucu sınırında tutuyorum.
export async function authenticatedApiRequest<T>(
  path: string,
  options: AuthenticatedRequestOptions = {},
): Promise<T> {
  const accessToken = await readAccessToken();
  if (!accessToken) {
    throw new ApiError({ title: "Oturum gerekli", status: 401, code: "authentication_required" });
  }

  const headers = new Headers({
    Accept: "application/json",
    Authorization: `Bearer ${accessToken}`,
  });
  if (options.body !== undefined) headers.set("Content-Type", "application/json");

  const timeoutSignal = AbortSignal.timeout(ACCOUNT_REQUEST_TIMEOUT_MS);
  const signal = options.signal ? AbortSignal.any([options.signal, timeoutSignal]) : timeoutSignal;
  const response = await fetch(internalApiUrl(path), {
    method: options.method ?? "GET",
    headers,
    body: options.body === undefined ? undefined : JSON.stringify(options.body),
    cache: "no-store",
    signal,
  });

  if (!response.ok) {
    const contentType = response.headers.get("content-type") || "";
    const responseBody = contentType.includes("json") ? await response.json().catch(() => null) : null;
    throw new ApiError(normalizeApiProblem(response.status, responseBody, response.headers.get("Retry-After") || undefined));
  }

  if (response.status === 204) return undefined as T;
  return await response.json() as T;
}
