import { afterEach, describe, expect, it, vi } from "vitest";

import {
  clearFavoriteStateForOwnerChange,
  favoriteErrorMessage,
  getFavoriteSnapshot,
  loadFavoriteState,
  mutateFavorite,
  resetFavoriteState,
} from "./favorites-api";

afterEach(() => {
  resetFavoriteState();
  vi.unstubAllGlobals();
});

describe("favorites client api", () => {
  // Burada çok sayıda ürün kontrolünün aynı anda yüklenirken tek favori liste isteğini paylaştığını doğruluyorum.
  it("deduplicates the favorite state request without product detail calls", async () => {
    const fetchMock = vi.fn(async (...args: Parameters<typeof fetch>) => {
      void args;
      return new Response(JSON.stringify({ productIds: ["P00001"], totalCount: 1 }), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      });
    });
    vi.stubGlobal("fetch", fetchMock);
    vi.stubGlobal("window", { dispatchEvent: vi.fn() });
    vi.stubGlobal("CustomEvent", class<T> {
      constructor(public type: string, public init: CustomEventInit<T>) {}
    });

    const navbarLoad = loadFavoriteState();
    const firstCardLoad = loadFavoriteState();
    const secondCardLoad = loadFavoriteState();
    await Promise.all([navbarLoad, firstCardLoad, secondCardLoad]);

    expect(fetchMock).toHaveBeenCalledOnce();
    expect(fetchMock.mock.calls[0]?.[0]).toBe("/api/favorites");
    expect(fetchMock.mock.calls[0]?.[0]).not.toContain("/products/");
  });

  // Burada 204 gövdesini ayrıştırmadan ekleme ve silme sonucunun ortak snapshot'a işlendiğini doğruluyorum.
  it("publishes add and remove results from bodyless 204 responses", async () => {
    const responses = [
      new Response(JSON.stringify({ productIds: [], totalCount: 0 }), { status: 200, headers: { "Content-Type": "application/json" } }),
      new Response(null, { status: 204 }),
      new Response(null, { status: 204 }),
    ];
    const fetchMock = vi.fn(async (...args: Parameters<typeof fetch>) => {
      void args;
      return responses.shift()!;
    });
    vi.stubGlobal("fetch", fetchMock);
    vi.stubGlobal("window", { dispatchEvent: vi.fn() });
    vi.stubGlobal("CustomEvent", class<T> {
      constructor(public type: string, public init: CustomEventInit<T>) {}
    });

    await loadFavoriteState();
    await mutateFavorite("P00001", true);
    expect(getFavoriteSnapshot()).toEqual({ productIds: ["P00001"], totalCount: 1 });

    await mutateFavorite("P00001", false);
    expect(getFavoriteSnapshot()).toEqual({ productIds: [], totalCount: 0 });
    expect(fetchMock.mock.calls[1]?.[1]).toMatchObject({ method: "POST" });
    expect(fetchMock.mock.calls[1]?.[1]).not.toHaveProperty("body");
    expect(fetchMock.mock.calls[2]?.[1]).toMatchObject({ method: "DELETE" });
    expect(fetchMock.mock.calls[2]?.[1]).not.toHaveProperty("body");
  });

  // Burada aynı ürün mutation'ı sürerken ikinci tıklamanın yeni bir HTTP isteği üretmediğini doğruluyorum.
  it("deduplicates concurrent mutations for the same product", async () => {
    let resolveRequest!: (response: Response) => void;
    const fetchMock = vi.fn(() => new Promise<Response>((resolve) => { resolveRequest = resolve; }));
    vi.stubGlobal("fetch", fetchMock);
    vi.stubGlobal("window", { dispatchEvent: vi.fn() });
    vi.stubGlobal("CustomEvent", class<T> {
      constructor(public type: string, public init: CustomEventInit<T>) {}
    });

    const first = mutateFavorite("P00001", true);
    const second = mutateFavorite("P00001", true);
    expect(first).toBe(second);
    expect(fetchMock).toHaveBeenCalledOnce();

    resolveRequest(new Response(null, { status: 204 }));
    await expect(first).resolves.toMatchObject({ productIds: ["P00001"] });
  });

  // Burada mutation 500 ile reddedildiğinde optimistic favori durumunun önceki snapshot'a geri döndüğünü doğruluyorum.
  it("rolls optimistic state back when a mutation fails", async () => {
    vi.stubGlobal("fetch", vi.fn(async () => new Response(JSON.stringify({ status: 500, title: "Hata" }), {
      status: 500,
      headers: { "Content-Type": "application/problem+json" },
    })));
    vi.stubGlobal("window", { dispatchEvent: vi.fn() });
    vi.stubGlobal("CustomEvent", class<T> {
      constructor(public type: string, public init: CustomEventInit<T>) {}
    });

    const mutation = mutateFavorite("P00001", true);
    expect(getFavoriteSnapshot().productIds).toEqual(["P00001"]);
    await expect(mutation).rejects.toMatchObject({ status: 500 });
    expect(getFavoriteSnapshot()).toEqual({ productIds: [], totalCount: 0 });
  });

  // Burada duplicate POST conflict cevabının favori=true durumunu koruduğunu ve güvenli metnini ürettiğini doğruluyorum.
  it("keeps the product favorited after duplicate conflict", async () => {
    vi.stubGlobal("fetch", vi.fn(async () => new Response(JSON.stringify({ status: 409, code: "conflict", title: "Conflict" }), {
      status: 409,
      headers: { "Content-Type": "application/problem+json" },
    })));
    vi.stubGlobal("window", { dispatchEvent: vi.fn() });
    vi.stubGlobal("CustomEvent", class<T> {
      constructor(public type: string, public init: CustomEventInit<T>) {}
    });

    await expect(mutateFavorite("P00001", true)).rejects.toMatchObject({ status: 409, code: "conflict" });
    expect(getFavoriteSnapshot().productIds).toEqual(["P00001"]);
    expect(favoriteErrorMessage({ status: 409 })).toBe("Ürün zaten favorilerinizde.");
  });

  // Burada logout/guest geçişinde önceki kullanıcının favori snapshot'ının sonraki oturuma sızmadığını doğruluyorum.
  it("clears the in-memory favorite cache for a guest session", async () => {
    vi.stubGlobal("fetch", vi.fn(async () => new Response(null, { status: 204 })));
    vi.stubGlobal("window", { dispatchEvent: vi.fn() });
    vi.stubGlobal("CustomEvent", class<T> {
      constructor(public type: string, public init: CustomEventInit<T>) {}
    });

    await mutateFavorite("P00001", true);
    clearFavoriteStateForOwnerChange();
    expect(getFavoriteSnapshot()).toEqual({ productIds: [], totalCount: 0 });
  });

  // Burada liste endpointi bozulsa bile karttaki favori mutation'ının POST isteğini bağımsız gönderebildiğini doğruluyorum.
  it("does not block the mutation when the favorite list request fails", async () => {
    const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      void init;
      if (input === "/api/favorites") {
        return new Response(JSON.stringify({ status: 500, title: "Liste alınamadı" }), {
          status: 500,
          headers: { "Content-Type": "application/problem+json" },
        });
      }

      return new Response(JSON.stringify({ productId: "P00001", isFavorite: true }), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      });
    });
    vi.stubGlobal("fetch", fetchMock);
    vi.stubGlobal("window", { dispatchEvent: vi.fn() });
    vi.stubGlobal("CustomEvent", class<T> {
      constructor(public type: string, public init: CustomEventInit<T>) {}
    });

    await expect(loadFavoriteState()).rejects.toMatchObject({ status: 500 });
    await expect(mutateFavorite("P00001", true)).resolves.toEqual({
      productIds: ["P00001"],
      totalCount: 1,
    });

    expect(fetchMock).toHaveBeenCalledTimes(2);
    expect(fetchMock.mock.calls[1]?.[0]).toBe("/api/favorites/P00001");
    expect(fetchMock.mock.calls[1]?.[1]).toMatchObject({ method: "POST" });
  });

  // Burada auth, stale kayıt, conflict ve rate-limit cevaplarının aynı genel hata metnine düşmediğini doğruluyorum.
  it("keeps authentication, not found, conflict and rate limit messages distinct", () => {
    expect(favoriteErrorMessage({ status: 401 })).toContain("Favori oturumu");
    expect(favoriteErrorMessage({ status: 403 })).toContain("güvenlik doğrulaması");
    expect(favoriteErrorMessage({ status: 404 })).toContain("bulunamıyor");
    expect(favoriteErrorMessage({ status: 409 })).toContain("zaten favorilerinizde");
    expect(favoriteErrorMessage({ status: 429 })).toContain("bekleyin");
  });
});
