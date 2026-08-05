import "server-only";

import { getInternalApiOrigin } from "@/lib/api/config";
import { ApiError, normalizeApiProblem } from "@/lib/api/problem";

type ApiRequestOptions = Omit<RequestInit, "body" | "cache" | "headers"> & {
  accessToken?: string;
  body?: unknown;
  headers?: Record<string, string>;
};

// Burada tüm backend çağrılarını tek server-only sınırından, no-store ve kontrollü timeout ile gönderiyorum.
export async function apiRequest<T>(path: string, options: ApiRequestOptions = {}): Promise<T> {
  if (!path.startsWith("/") || path.startsWith("//")) {
    throw new Error("API path must be an application-owned relative path.");
  }

  const { accessToken, body, headers: requestHeaders, ...requestInit } = options;
  const headers = new Headers(requestHeaders);
  if (headers.has("Authorization")) {
    throw new Error("Authorization must be supplied through the server-only accessToken option.");
  }
  headers.set("Accept", "application/json");

  if (body !== undefined) {
    headers.set("Content-Type", "application/json");
  }

  if (accessToken) {
    headers.set("Authorization", `Bearer ${accessToken}`);
  }

  let response: Response;
  try {
    response = await fetch(new URL(path, getInternalApiOrigin()), {
      ...requestInit,
      body: body === undefined ? undefined : JSON.stringify(body),
      headers,
      cache: "no-store",
      redirect: "manual",
      signal: AbortSignal.timeout(12_000),
    });
  } catch (error) {
    throw new ApiError({
      title: "API bağlantısı kurulamadı",
      status: 503,
      code: error instanceof DOMException && error.name === "TimeoutError" ? "request_timeout" : "network_error",
      detail: "API geçici olarak yanıt vermiyor. Lütfen tekrar deneyin.",
    });
  }

  if (!response.ok) {
    const contentType = response.headers.get("content-type") || "";
    const payload = contentType.includes("json") ? await response.json().catch(() => null) : null;
    throw new ApiError(normalizeApiProblem(payload, response.status));
  }

  if (response.status === 204) {
    return undefined as T;
  }

  const contentType = response.headers.get("content-type") || "";
  if (!contentType.includes("json")) {
    throw new ApiError({
      title: "Geçersiz API yanıtı",
      status: 502,
      code: "invalid_upstream_response",
      detail: "API beklenen JSON yanıtını döndürmedi.",
    });
  }

  return (await response.json()) as T;
}
