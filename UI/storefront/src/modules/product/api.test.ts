import { beforeEach, describe, expect, it, vi } from "vitest";

const { apiGetMock } = vi.hoisted(() => ({ apiGetMock: vi.fn() }));

vi.mock("server-only", () => ({}));
vi.mock("@/lib/api/client", () => ({ apiGet: apiGetMock }));

import { getPublishedProductBySlug } from "@/modules/product/api";

describe("product API", () => {
  beforeEach(() => {
    apiGetMock.mockReset();
    apiGetMock.mockResolvedValue({ product: {}, images: [], lastModifiedAt: "2026-08-22T00:00:00Z" });
  });

  // Burada canlı fiyat, stok ve varyant taşıyan ürün detayının istekler arasında eski Next cache verisi kullanmadığını doğruluyorum.
  it("fetches published product detail without persistent cache", async () => {
    await getPublishedProductBySlug("organik-recine-yuzuk");

    expect(apiGetMock).toHaveBeenCalledWith(
      "/api/products/by-url/organik-recine-yuzuk",
      { cache: "no-store" },
    );
  });
});
