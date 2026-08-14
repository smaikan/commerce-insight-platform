import { beforeEach, describe, expect, it, vi } from "vitest";

const { forwardMutationMock, trustedOriginMock } = vi.hoisted(() => ({
  forwardMutationMock: vi.fn(),
  trustedOriginMock: vi.fn(),
}));

vi.mock("server-only", () => ({}));
vi.mock("@/lib/security/storefront-origin", () => ({ hasTrustedStorefrontOrigin: trustedOriginMock }));
vi.mock("@/modules/favorites/server/favorite-proxy", () => ({
  forwardFavoriteMutationRequest: forwardMutationMock,
}));

import { DELETE, POST } from "./route";

const context = { params: Promise.resolve({ productId: "P00001" }) };

describe("favorite mutation BFF route", () => {
  beforeEach(() => {
    trustedOriginMock.mockReset().mockReturnValue(true);
    forwardMutationMock.mockReset().mockResolvedValue(new Response(null, {
      status: 204,
      headers: { "Cache-Control": "private, no-store" },
    }));
  });

  // Burada guest ekleme isteğinin ürün ve method ile owner-aware proxy'ye tek kez iletildiğini doğruluyorum.
  it("forwards a favorite add once", async () => {
    const request = new Request("http://localhost/api/favorites/P00001", {
      method: "POST",
      headers: { Origin: "http://localhost" },
    });
    const response = await POST(request, context);

    expect(forwardMutationMock).toHaveBeenCalledOnce();
    expect(forwardMutationMock).toHaveBeenCalledWith(request, "P00001", "POST");
    expect(response.status).toBe(204);
  });

  // Burada guest silme isteğinin aynı güvenlik sınırından yalnız bir kez geçtiğini doğruluyorum.
  it("forwards a favorite delete once", async () => {
    const request = new Request("http://localhost/api/favorites/P00001", {
      method: "DELETE",
      headers: { Origin: "http://localhost" },
    });
    const response = await DELETE(request, context);

    expect(forwardMutationMock).toHaveBeenCalledWith(request, "P00001", "DELETE");
    expect(response.status).toBe(204);
  });

  // Burada güvenilmeyen browser Origin değerini upstream ve guest tokenına ulaşmadan reddediyorum.
  it("rejects an untrusted browser origin", async () => {
    trustedOriginMock.mockReturnValue(false);
    const response = await POST(new Request("http://localhost/api/favorites/P00001", { method: "POST" }), context);

    expect(response.status).toBe(403);
    expect(forwardMutationMock).not.toHaveBeenCalled();
  });
});
