import { beforeEach, describe, expect, it, vi } from "vitest";

const { readAccessTokenMock } = vi.hoisted(() => ({ readAccessTokenMock: vi.fn() }));

vi.mock("server-only", () => ({}));
vi.mock("@/lib/auth/cookies", () => ({ readAccessToken: readAccessTokenMock }));

import {
  forwardFavoriteMutationRequest,
  forwardFavoriteStateRequest,
} from "@/modules/favorites/server/favorite-proxy";

const guestToken = "A".repeat(64);
const setCookie = `ecommerce_guest_cart=${guestToken}; Path=/api; HttpOnly; Secure; SameSite=Lax; Max-Age=2592000`;

describe("favorite owner-aware BFF proxy", () => {
  beforeEach(() => {
    vi.restoreAllMocks();
    readAccessTokenMock.mockReset().mockResolvedValue(null);
  });

  // Burada ilk guest POST öncesinde sessionı GET ile kurup mutationı cookie-Origin-CSRF üçlüsüyle yalnız bir kez gönderiyorum.
  it("bootstraps and forwards the first guest mutation securely", async () => {
    const fetchMock = vi.spyOn(globalThis, "fetch")
      .mockResolvedValueOnce(new Response(JSON.stringify(emptyPage()), {
        status: 200,
        headers: { "Content-Type": "application/json", "Set-Cookie": setCookie },
      }))
      .mockResolvedValueOnce(new Response(null, { status: 204 }));

    const response = await forwardFavoriteMutationRequest(
      new Request("http://localhost:3000/api/favorites/P00001", { method: "POST" }),
      "P00001",
      "POST",
    );

    expect(fetchMock).toHaveBeenCalledTimes(2);
    const mutationHeaders = fetchMock.mock.calls[1]?.[1]?.headers as Headers;
    expect(mutationHeaders.get("Cookie")).toBe(`ecommerce_guest_cart=${guestToken}`);
    expect(mutationHeaders.get("Origin")).toBe("http://localhost:3000");
    expect(mutationHeaders.get("X-Guest-CSRF")).toBe(guestToken);
    expect(response.status).toBe(204);
    expect(response.headers.get("Cache-Control")).toBe("private, no-store");
    expect(response.headers.get("Set-Cookie")).toContain("HttpOnly");
    expect(await response.text()).toBe("");
  });

  // Burada mevcut guest session ile mutation öncesi ek GET üretmeden tek upstream çağrısı yaptığımı doğruluyorum.
  it("does not duplicate a mutation when the guest cookie already exists", async () => {
    const fetchMock = vi.spyOn(globalThis, "fetch").mockResolvedValueOnce(new Response(null, { status: 204 }));

    await forwardFavoriteMutationRequest(new Request("http://localhost:3000/api/favorites/P00001", {
      method: "POST",
      headers: { Cookie: `ecommerce_guest_cart=${guestToken}` },
    }), "P00001", "POST");

    expect(fetchMock).toHaveBeenCalledOnce();
  });

  // Burada JWT varken guest cookie'yi tamamen yok sayıp yalnız bearer sahipliğini upstream'e taşıyorum.
  it("gives authenticated JWT ownership priority over a guest cookie", async () => {
    readAccessTokenMock.mockResolvedValue("access-token");
    const fetchMock = vi.spyOn(globalThis, "fetch").mockResolvedValueOnce(new Response(null, { status: 204 }));

    await forwardFavoriteMutationRequest(new Request("http://localhost:3000/api/favorites/P00001", {
      method: "POST",
      headers: { Cookie: `ecommerce_guest_cart=${guestToken}` },
    }), "P00001", "POST");

    const headers = fetchMock.mock.calls[0]?.[1]?.headers as Headers;
    expect(headers.get("Authorization")).toBe("Bearer access-token");
    expect(headers.has("Cookie")).toBe(false);
    expect(headers.has("X-Guest-CSRF")).toBe(false);
  });

  // Burada duplicate 409 cevabını otomatik mutation retry yapmadan private ProblemDetails olarak koruyorum.
  it("preserves a duplicate 409 without retrying", async () => {
    const fetchMock = vi.spyOn(globalThis, "fetch").mockResolvedValueOnce(new Response(JSON.stringify({
      status: 409,
      title: "Conflict",
      code: "conflict",
    }), { status: 409, headers: { "Content-Type": "application/problem+json" } }));

    const response = await forwardFavoriteMutationRequest(new Request("http://localhost:3000/api/favorites/P00001", {
      method: "POST",
      headers: { Cookie: `ecommerce_guest_cart=${guestToken}` },
    }), "P00001", "POST");

    expect(fetchMock).toHaveBeenCalledOnce();
    expect(response.status).toBe(409);
    expect(response.headers.get("Cache-Control")).toBe("private, no-store");
  });

  // Burada ilk guest GET cevabından session cookie'sini geçirip favori state'ini ürün kimliklerine indiriyorum.
  it("returns guest favorite state and forwards the session cookie", async () => {
    vi.spyOn(globalThis, "fetch").mockResolvedValueOnce(new Response(JSON.stringify({
      ...emptyPage(),
      items: [{ id: "P00001" }],
      totalCount: 1,
    }), { status: 200, headers: { "Content-Type": "application/json", "Set-Cookie": setCookie } }));

    const response = await forwardFavoriteStateRequest(new Request("http://localhost:3000/api/favorites"));

    expect(response.status).toBe(200);
    await expect(response.json()).resolves.toEqual({ productIds: ["P00001"], totalCount: 1 });
    expect(response.headers.get("Set-Cookie")).toContain("ecommerce_guest_cart=");
  });
});

// Burada testlerde API'nin boş ProductDtoPagedResult sözleşmesini tek yerde oluşturuyorum.
function emptyPage() {
  return {
    items: [],
    pageNumber: 1,
    pageSize: 100,
    totalCount: 0,
    totalPages: 0,
    hasPreviousPage: false,
    hasNextPage: false,
  };
}
