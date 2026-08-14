import { describe, expect, it } from "vitest";

import {
  catalogHref,
  catalogCanonicalHref,
  catalogHrefWithoutFilter,
  catalogSearchParamsNeedRedirect,
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

  // Burada arama metninin API Search alanına taşındığını ve seçim yokken relevance'ı ezebilecek SortBy üretilmediğini doğruluyorum.
  it("preserves backend relevance when search has no explicit sort", () => {
    const view = parseCatalogView({ q: "  inci   kolye " });

    expect(view).toEqual({ page: 1, sort: "newest", search: "inci kolye" });
    expect(toPublishedProductQuery(view)).toEqual({
      PageNumber: 1,
      PageSize: 24,
      Search: "inci kolye",
    });
  });

  // Burada kullanıcı sıralama seçtiğinde Search ile numeric SortBy değerinin birlikte API'ye gönderildiğini doğruluyorum.
  it("keeps search when the user explicitly sorts", () => {
    const view = parseCatalogView({ q: "inci", sort: "popular", page: "2" });

    expect(toPublishedProductQuery(view)).toEqual({
      PageNumber: 2,
      PageSize: 24,
      SortBy: 1,
      Descending: true,
      Search: "inci",
    });
    expect(catalogHref(view)).toBe("/products?q=inci&page=2&sort=popular");
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

  // Burada sınıflandırma yolunda temsil edilen filtrenin sorguya tekrarlanmadığını doğruluyorum.
  it("creates classification links without duplicating the path-owned filter", () => {
    expect(catalogHref({
      page: 2,
      sort: "popular",
      brandId: "90889aa5-e32a-48d9-a16a-90d663def971",
      typeId: "21ee9c6b-f6d2-4aae-9672-44ed04eda316",
    }, {
      basePath: "/brand/serantis",
      omitFilter: "brandId",
    })).toBe("/brand/serantis?page=2&sort=popular&type=21ee9c6b-f6d2-4aae-9672-44ed04eda316");
  });

  // Burada geçersiz veya yolda zaten temsil edilen parametrelerin temiz URL yönlendirmesini tetiklediğini doğruluyorum.
  it("detects search params that need a clean classification redirect", () => {
    const view = {
      page: 1,
      sort: "newest" as const,
      brandId: "90889aa5-e32a-48d9-a16a-90d663def971",
    };
    const options = { basePath: "/brand/serantis", omitFilter: "brandId" as const };

    expect(catalogSearchParamsNeedRedirect({}, view, options)).toBe(false);
    expect(catalogSearchParamsNeedRedirect({ page: "1" }, view, options)).toBe(true);
    expect(catalogSearchParamsNeedRedirect({ brand: view.brandId }, view, options)).toBe(true);
    expect(catalogSearchParamsNeedRedirect({ sort: ["popular", "title"] }, { ...view, sort: "popular" }, options)).toBe(true);
  });

  // Burada aramanın sayfalama ve sınıflandırma filtreleri değişirken görünür URL'de korunduğunu doğruluyorum.
  it("keeps search across pagination and filters", () => {
    expect(catalogHref({
      page: 3,
      sort: "newest",
      search: "gözlük zinciri",
      brandId: "90889aa5-e32a-48d9-a16a-90d663def971",
    })).toBe("/products?q=g%C3%B6zl%C3%BCk+zinciri&page=3&brand=90889aa5-e32a-48d9-a16a-90d663def971");
  });

  // Burada sınıflandırma landing sayfasının sabit ID filtresini query'ye sızdırmadan sıralama ve sayfalama bilgisini koruduğunu doğruluyorum.
  it("creates clean classification landing links", () => {
    expect(catalogHref({
      page: 2,
      sort: "popular",
      collectionId: "6681db54-98f3-4ee6-899a-3348c5f20517",
      brandId: "90889aa5-e32a-48d9-a16a-90d663def971",
    }, {
      basePath: "/collection/taki",
      omitFilter: "collectionId",
    })).toBe(
      "/collection/taki?page=2&sort=popular&brand=90889aa5-e32a-48d9-a16a-90d663def971",
    );
  });

  // Burada native filtre formunun boş/default alanlarının temiz URL yönlendirmesi gerektirdiğini doğruluyorum.
  it("detects catalog query values that should be removed from the visible URL", () => {
    expect(catalogSearchParamsNeedRedirect(
      { brand: "", collection: "", type: "", page: "1", sort: "newest" },
      { page: 1, sort: "newest" },
    )).toBe(true);
    expect(catalogSearchParamsNeedRedirect(
      { brand: "90889aa5-e32a-48d9-a16a-90d663def971" },
      { page: 1, sort: "newest", brandId: "90889aa5-e32a-48d9-a16a-90d663def971" },
    )).toBe(false);
    expect(catalogSearchParamsNeedRedirect(
      { collection: "6681db54-98f3-4ee6-899a-3348c5f20517" },
      { page: 1, sort: "newest", collectionId: "6681db54-98f3-4ee6-899a-3348c5f20517" },
      { basePath: "/collection/taki", omitFilter: "collectionId" },
    )).toBe(true);
  });

  // Burada sıralama kopyasının canonical URL'sinden yalnız sıralamayı çıkarıp gerçek sayfayı koruduğumu doğruluyorum.
  it("preserves pagination while canonicalizing alternate sorting", () => {
    expect(catalogCanonicalHref({ page: 3, sort: "popular" })).toBe("/products?page=3");
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
