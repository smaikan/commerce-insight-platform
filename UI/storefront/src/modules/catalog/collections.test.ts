import { beforeEach, describe, expect, it, vi } from "vitest";

const apiGetMock = vi.hoisted(() => vi.fn());

vi.mock("server-only", () => ({}));
vi.mock("@/lib/api/client", () => ({ apiGet: apiGetMock }));

import { getCollectionShowcase } from "./collections";

describe("collection showcase data", () => {
  beforeEach(() => {
    apiGetMock.mockReset();
  });

  // Burada tek published koleksiyon isteğini, API sayfalamasını ve backend sırasının değişmeden korunduğunu doğruluyorum.
  it("uses the published showcase endpoint once and preserves its response", async () => {
    apiGetMock.mockResolvedValue({
      items: [
        {
          id: "second-by-name",
          name: "Zümrüt Seçkisi",
          url: "backend-owned-path",
          productCount: 7,
          isFeatured: true,
          displayOrder: 1,
          imageUrl: "https://res.cloudinary.com/demo/showcase.jpg",
        },
        {
          id: "first-by-name",
          name: "Ada",
          url: "ada-canonical",
          productCount: 2,
          isFeatured: false,
          displayOrder: 2,
          imageUrl: null,
        },
      ],
      pageNumber: 2,
      pageSize: 35,
      totalCount: 37,
      totalPages: 2,
      hasPreviousPage: true,
      hasNextPage: false,
    });

    const result = await getCollectionShowcase(2, 35);

    expect(apiGetMock).toHaveBeenCalledTimes(1);
    expect(apiGetMock).toHaveBeenCalledWith(
      "/api/collections/published?PageNumber=2&PageSize=35",
      { revalidate: 30, tags: ["published-collections"] },
    );
    expect(apiGetMock.mock.calls.some(([path]) => String(path).includes("/api/products/published"))).toBe(false);
    expect(apiGetMock.mock.calls.some(([path]) => String(path).includes("/facets/"))).toBe(false);
    expect(result.items.map(({ id }) => id)).toEqual(["second-by-name", "first-by-name"]);
    expect(result.items[0]).toMatchObject({
      name: "Zümrüt Seçkisi",
      href: "/collection/backend-owned-path",
      imageUrl: "https://res.cloudinary.com/demo/showcase.jpg",
      imageAlt: "Zümrüt Seçkisi",
      productCount: 7,
      isFeatured: true,
    });
    expect(result.items[1]).toMatchObject({
      href: "/collection/ada-canonical",
      imageUrl: null,
      productCount: 2,
    });
  });
});
