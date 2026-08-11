import { describe, expect, it } from "vitest";

import {
  catalogHref,
  catalogHrefWithoutFilter,
  parseCatalogView,
  toPublishedProductQuery,
} from "./query";

describe("catalog query", () => {
  // Burada geçersiz URL değerlerinin güvenli katalog varsayılanlarına döndüğünü doğruluyorum.
  it("normalizes invalid page and sort values", () => {
    expect(parseCatalogView({ page: "-4", sort: "unknown" })).toEqual({ page: 1, sort: "newest" });
  });

  // Burada popüler sıralamanın backend numeric enum ve yön değerine doğru çevrildiğini doğruluyorum.
  it("maps popular sorting to the documented API query", () => {
    expect(toPublishedProductQuery({ page: 2, sort: "popular" })).toEqual({
      PageNumber: 2,
      PageSize: 24,
      SortBy: 1,
      Descending: true,
    });
  });

  // Burada üç sınıflandırma filtresinin yalnız geçerli GUID değerleriyle backend sorgusuna taşındığını doğruluyorum.
  it("maps valid classification filters to the published products query", () => {
    const view = parseCatalogView({
      brand: "90889aa5-e32a-48d9-a16a-90d663def971",
      collection: "6681db54-98f3-4ee6-899a-3348c5f20517",
      type: "21ee9c6b-f6d2-4aae-9672-44ed04eda316",
    });

    expect(toPublishedProductQuery(view)).toMatchObject({
      BrandId: "90889aa5-e32a-48d9-a16a-90d663def971",
      CollectionId: "6681db54-98f3-4ee6-899a-3348c5f20517",
      TypeId: "21ee9c6b-f6d2-4aae-9672-44ed04eda316",
    });
  });

  // Burada bozuk filtre değerlerinin URL ve API sorgusuna taşınmadığını doğruluyorum.
  it("drops malformed classification filters", () => {
    expect(parseCatalogView({ brand: "not-a-guid", collection: "", type: ["bad", "also-bad"] })).toEqual({
      page: 1,
      sort: "newest",
    });
  });

  // Burada varsayılan parametreleri temiz, diğerlerini paylaşılabilir URL olarak ürettiğimi doğruluyorum.
  it("creates minimal catalog links", () => {
    expect(catalogHref({ page: 1, sort: "newest" })).toBe("/products");
    expect(catalogHref({ page: 3, sort: "title" })).toBe("/products?page=3&sort=title");
    expect(catalogHref({
      page: 1,
      sort: "newest",
      brandId: "90889aa5-e32a-48d9-a16a-90d663def971",
    })).toBe("/products?brand=90889aa5-e32a-48d9-a16a-90d663def971");
  });

  // Burada tek filtre kaldırıldığında diğer seçimlerin ve sıralamanın korunup sayfanın bire döndüğünü doğruluyorum.
  it("removes one classification filter without losing the others", () => {
    expect(catalogHrefWithoutFilter({
      page: 3,
      sort: "popular",
      brandId: "90889aa5-e32a-48d9-a16a-90d663def971",
      collectionId: "6681db54-98f3-4ee6-899a-3348c5f20517",
      typeId: "21ee9c6b-f6d2-4aae-9672-44ed04eda316",
    }, "collectionId")).toBe(
      "/products?sort=popular&brand=90889aa5-e32a-48d9-a16a-90d663def971&type=21ee9c6b-f6d2-4aae-9672-44ed04eda316",
    );
  });
});
