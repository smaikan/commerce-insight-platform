import { beforeEach, describe, expect, it, vi } from "vitest";

const apiGetMock = vi.hoisted(() => vi.fn());

vi.mock("server-only", () => ({}));
vi.mock("@/lib/api/client", () => ({ apiGet: apiGetMock }));

import { getCategoryShowcase, getMostPopulatedCategories } from "./categories";
import { parseCatalogView, toPublishedProductQuery } from "./query";

const ringId = "11111111-1111-4111-8111-111111111111";
const necklaceId = "22222222-2222-4222-8222-222222222222";

describe("category showcase data", () => {
  beforeEach(() => {
    apiGetMock.mockReset();
  });

  // Burada özel görseli, backend fallback görselini ve null değeri tek published kategori isteğiyle koruduğumu doğruluyorum.
  it("uses the published endpoint once and trusts backend-owned images and counts", async () => {
    apiGetMock.mockResolvedValue({
      items: [
        {
          id: ringId,
          name: "Özel & Yüzükler",
          productCount: 9,
          imageUrl: "https://res.cloudinary.com/demo/category-custom.jpg",
        },
        {
          id: necklaceId,
          name: "Kolyeler",
          productCount: 4,
          imageUrl: "https://res.cloudinary.com/demo/backend-product-fallback.jpg",
        },
        {
          id: "33333333-3333-4333-8333-333333333333",
          name: "Görselsiz",
          productCount: 1,
          imageUrl: null,
        },
      ],
      pageNumber: 1,
      pageSize: 20,
      totalCount: 3,
      totalPages: 1,
      hasPreviousPage: false,
      hasNextPage: false,
    });

    const result = await getCategoryShowcase(1, 20);

    expect(apiGetMock).toHaveBeenCalledTimes(1);
    expect(apiGetMock).toHaveBeenCalledWith(
      "/api/product-types/published?PageNumber=1&PageSize=20",
      { revalidate: 30, tags: ["published-product-types"] },
    );
    expect(apiGetMock.mock.calls.some(([path]) => String(path).includes("/api/products"))).toBe(false);
    expect(result.items[0]).toMatchObject({
      href: `/products?type=${ringId}`,
      imageAlt: "Özel & Yüzükler",
      imageUrl: "https://res.cloudinary.com/demo/category-custom.jpg",
      productCount: 9,
    });
    expect(result.items[1].imageUrl).toBe("https://res.cloudinary.com/demo/backend-product-fallback.jpg");
    expect(result.items[2].imageUrl).toBeNull();
    expect(result.items[0].href).not.toContain("ozel");

    // Burada kartın taşıdığı type parametresinin gerçek API sorgusunda TypeId alanına dönüştüğünü doğruluyorum.
    const apiQuery = toPublishedProductQuery(parseCatalogView({ type: ringId }));
    expect(apiQuery.TypeId).toBe(ringId);
  });

  // Burada kategori sayısı büyüdüğünde veri katmanının kategori veya ürün başına yeni istek üretmediğini doğruluyorum.
  it("keeps a single request as the category count grows", async () => {
    apiGetMock.mockResolvedValue({
      items: Array.from({ length: 50 }, (_, index) => ({
        id: `00000000-0000-4000-8000-${String(index).padStart(12, "0")}`,
        name: `Kategori ${index}`,
        productCount: index + 1,
        imageUrl: null,
      })),
      pageNumber: 1,
      pageSize: 50,
      totalCount: 50,
      totalPages: 1,
      hasPreviousPage: false,
      hasNextPage: false,
    });

    const result = await getCategoryShowcase(1, 50);

    expect(result.items).toHaveLength(50);
    expect(apiGetMock).toHaveBeenCalledTimes(1);
    expect(apiGetMock.mock.calls.every(([path]) => String(path).startsWith("/api/product-types/published"))).toBe(true);
  });

  it("selects the globally highest product counts and links them to crawlable category landings", async () => {
    apiGetMock.mockResolvedValue({
      items: [
        { id: ringId, name: "Yüzük", productCount: 5, imageUrl: "https://cdn.example.com/ring.webp" },
        { id: necklaceId, name: "Kolye", productCount: 9, imageUrl: "https://cdn.example.com/necklace.webp" },
        { id: "empty", name: "Boş", productCount: 0, imageUrl: null },
      ],
      pageNumber: 1,
      pageSize: 100,
      totalCount: 3,
      totalPages: 1,
      hasPreviousPage: false,
      hasNextPage: false,
    });

    const result = await getMostPopulatedCategories(2);

    expect(result.map((item) => item.name)).toEqual(["Kolye", "Yüzük"]);
    expect(result.map((item) => item.href)).toEqual(["/category/kolye", "/category/yuzuk"]);
  });
});
