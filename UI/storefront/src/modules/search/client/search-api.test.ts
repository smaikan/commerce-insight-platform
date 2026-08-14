import { afterEach, describe, expect, it, vi } from "vitest";

import { requestSearchSuggestions, searchErrorMessage } from "./search-api";

afterEach(() => {
  vi.unstubAllGlobals();
});

describe("search client api", () => {
  // Burada suggestion isteğinin sorguyu tek same-origin çağrıda taşıdığını ve signal'ı fetch'e aktardığını doğruluyorum.
  it("forwards the query and abort signal without product-level requests", async () => {
    const controller = new AbortController();
    const fetchMock = vi.fn(async (...args: Parameters<typeof fetch>) => {
      void args;
      return new Response(JSON.stringify({ items: [], hasMore: false }), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      });
    });
    vi.stubGlobal("fetch", fetchMock);

    await requestSearchSuggestions("inci kolye", controller.signal);

    expect(fetchMock).toHaveBeenCalledOnce();
    expect(fetchMock.mock.calls[0]?.[0]).toBe("/api/search/suggestions?q=inci%20kolye");
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ signal: controller.signal, cache: "no-store" });
  });

  // Burada 429 ProblemDetails cevabının otomatik retry yerine bekleme mesajına dönüştüğünü doğruluyorum.
  it("maps rate limiting to a dedicated waiting message", async () => {
    vi.stubGlobal("fetch", vi.fn(async () => new Response(JSON.stringify({ title: "Too many requests" }), {
      status: 429,
      headers: { "Content-Type": "application/problem+json", "Retry-After": "30" },
    })));

    const error = await requestSearchSuggestions("inci", new AbortController().signal).catch((value) => value);

    expect(error).toMatchObject({ status: 429, retryAfter: "30" });
    expect(searchErrorMessage(error)).toContain("kısa bir süre bekleyip");
  });

  // Burada 400 doğrulama ve genel bağlantı hatalarının rate-limit mesajıyla karışmadan açıklanabildiğini doğruluyorum.
  it("keeps validation and generic connection messages distinct", () => {
    expect(searchErrorMessage({ status: 400, detail: "Arama metni geçersiz." })).toBe("Arama metni geçersiz.");
    expect(searchErrorMessage(new Error("offline"))).toContain("Bağlantınızı kontrol edip");
  });
});
