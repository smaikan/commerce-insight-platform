import { beforeEach, describe, expect, it, vi } from "vitest";

const { readAccessTokenMock, readRefreshTokenMock } = vi.hoisted(() => ({
  readAccessTokenMock: vi.fn(),
  readRefreshTokenMock: vi.fn(),
}));

vi.mock("server-only", () => ({}));
vi.mock("@/lib/auth/cookies", () => ({
  readAccessToken: readAccessTokenMock,
  readRefreshToken: readRefreshTokenMock,
}));

import { forwardCartRequest } from "@/modules/cart/server/cart-proxy";

const guestToken = "A".repeat(64);

describe("cart owner-aware BFF proxy", () => {
  beforeEach(() => {
    vi.restoreAllMocks();
    readAccessTokenMock.mockReset().mockResolvedValue(null);
    readRefreshTokenMock.mockReset().mockResolvedValue(null);
  });

  // Burada auth oturumu olmayan istekte yalnız allowlist edilmiş guest cookie'sini upstream sepetine taşıyorum.
  it("forwards the guest cart cookie without an authorization header", async () => {
    const fetchMock = vi.spyOn(globalThis, "fetch").mockResolvedValueOnce(cartResponse(1, {
      "Set-Cookie": `ecommerce_guest_cart=${guestToken}; Path=/api; HttpOnly; SameSite=Lax`,
    }));

    const response = await forwardCartRequest(new Request("http://localhost:3000/api/cart", {
      headers: { Cookie: `ecommerce_guest_cart=${guestToken}; unrelated=value` },
    }), "/api/cart", { method: "GET" });

    const headers = fetchMock.mock.calls[0]?.[1]?.headers as Headers;
    expect(headers.get("Cookie")).toBe(`ecommerce_guest_cart=${guestToken}`);
    expect(headers.has("Authorization")).toBe(false);
    expect(response.headers.get("Set-Cookie")).toContain("ecommerce_guest_cart=");
  });

  // Burada login sonrasında JWT sahipliğini guest cookie'den öncelikli tutup iki kimliği aynı upstream isteğinde karıştırmıyorum.
  it("forwards only the bearer token for an authenticated cart", async () => {
    readAccessTokenMock.mockResolvedValue("access-token");
    readRefreshTokenMock.mockResolvedValue("refresh-token");
    const fetchMock = vi.spyOn(globalThis, "fetch").mockResolvedValueOnce(cartResponse(2, {
      "Set-Cookie": `ecommerce_guest_cart=${guestToken}; Path=/api; HttpOnly; SameSite=Lax`,
    }));

    const response = await forwardCartRequest(new Request("http://localhost:3000/api/cart", {
      headers: { Cookie: `ecommerce_guest_cart=${guestToken}` },
    }), "/api/cart", { method: "GET" });

    const headers = fetchMock.mock.calls[0]?.[1]?.headers as Headers;
    expect(headers.get("Authorization")).toBe("Bearer access-token");
    expect(headers.has("Cookie")).toBe(false);
    expect(response.headers.has("Set-Cookie")).toBe(false);
    await expect(response.json()).resolves.toMatchObject({ totalQuantity: 2 });
  });

  // Burada yalnız refresh cookie kaldığında yeni guest sepeti üretmeden client'ı kontrollü oturum yenilemesine yönlendiriyorum.
  it("requires refresh instead of falling back to a guest cart", async () => {
    readRefreshTokenMock.mockResolvedValue("refresh-token");
    const fetchMock = vi.spyOn(globalThis, "fetch");

    const response = await forwardCartRequest(new Request("http://localhost:3000/api/cart", {
      headers: { Cookie: `ecommerce_guest_cart=${guestToken}` },
    }), "/api/cart", { method: "GET" });

    expect(fetchMock).not.toHaveBeenCalled();
    expect(response.status).toBe(401);
    await expect(response.json()).resolves.toMatchObject({ code: "session_refresh_required" });
  });

  // Burada API'nin authenticated 401 cevabında guest isteği denemeden tek refresh sözleşmesini koruyorum.
  it("does not retry an expired authenticated cart as guest", async () => {
    readAccessTokenMock.mockResolvedValue("expired-access-token");
    const fetchMock = vi.spyOn(globalThis, "fetch").mockResolvedValueOnce(new Response(JSON.stringify({
      status: 401,
      title: "Unauthorized",
      code: "invalid_access_token",
      traceId: "safe-trace-id",
    }), { status: 401, headers: { "Content-Type": "application/problem+json" } }));

    const response = await forwardCartRequest(new Request("http://localhost:3000/api/cart"), "/api/cart", { method: "GET" });

    expect(fetchMock).toHaveBeenCalledOnce();
    expect(response.status).toBe(401);
    await expect(response.json()).resolves.toMatchObject({
      code: "session_refresh_required",
      traceId: "safe-trace-id",
    });
  });
});

// Burada proxy testinin yalnız sahiplik aktarımına odaklanması için küçük bir otoriter CartDto cevabı üretiyorum.
function cartResponse(totalQuantity: number, headers: HeadersInit = {}): Response {
  return new Response(JSON.stringify({
    id: "6cf01506-270c-45a8-8d0c-e957a2ae873c",
    concurrencyToken: "8d52d55c-1acd-4c54-a9a0-3354e9f0d263",
    items: [],
    totalQuantity,
    subTotal: 0,
    hasUnavailableItems: false,
    hasPriceChanges: false,
  }), { status: 200, headers: { "Content-Type": "application/json", ...headers } });
}
