import { renderToStaticMarkup } from "react-dom/server";
import { describe, expect, it } from "vitest";

import { CatalogFilters } from "./catalog-filters";

const brandId = "90889aa5-e32a-48d9-a16a-90d663def971";
const collectionId = "6681db54-98f3-4ee6-899a-3348c5f20517";
const typeId = "21ee9c6b-f6d2-4aae-9672-44ed04eda316";

const facets = {
  brands: [{ id: brandId, name: "SERANTIS", url: "serantis", isActive: true }],
  collections: [{ id: collectionId, name: "Aksesuarlar", url: "aksesuarlar", isActive: true, isFeatured: false, displayOrder: 0 }],
  productTypes: [{ id: typeId, name: "Accessory", isActive: true }],
};

describe("catalog filters", () => {
  // Burada seçili marka, koleksiyon ve ürün türünün panel kapalıyken de görünür ve ayrı ayrı kaldırılabilir olduğunu doğruluyorum.
  it("renders named removable active filters", () => {
    const markup = renderToStaticMarkup(
      <CatalogFilters
        facets={facets}
        view={{ page: 2, sort: "popular", brandId, collectionId, typeId }}
      />,
    );

    expect(markup).toContain("3 seçili");
    expect(markup).toContain("Marka: SERANTIS filtresini kaldır");
    expect(markup).toContain("Koleksiyon: Aksesuarlar filtresini kaldır");
    expect(markup).toContain("Ürün türü: Accessory filtresini kaldır");
    expect(markup).toContain(`sort=popular&amp;collection=${collectionId}&amp;type=${typeId}`);
  });

  // Burada select alanlarının genel “Tümü” yerine hangi filtre grubunu temizlediğini açıkça anlattığını doğruluyorum.
  it("renders specific empty option labels", () => {
    const markup = renderToStaticMarkup(
      <CatalogFilters facets={facets} view={{ page: 1, sort: "newest" }} />,
    );

    expect(markup).toContain("Tüm markalar");
    expect(markup).toContain("Tüm koleksiyonlar");
    expect(markup).toContain("Tüm ürün türleri");
  });

  // Burada sınıflandırma sayfasında yolun sahibi olan filtrenin yinelenmediğini ve diğer filtrelerin aynı yolda kaldığını doğruluyorum.
  it("keeps classification filters on their own route", () => {
    const markup = renderToStaticMarkup(
      <CatalogFilters
        facets={facets}
        view={{ page: 1, sort: "popular", brandId, collectionId }}
        urlOptions={{ basePath: "/brand/serantis", omitFilter: "brandId" }}
      />,
    );

    expect(markup).toContain('action="/brand/serantis"');
    expect(markup).not.toContain('name="brand"');
    expect(markup).not.toContain("Marka: SERANTIS filtresini kaldır");
    expect(markup).toContain("/brand/serantis?sort=popular");
  });

  // Burada arama sonucu üzerinde filtre uygulanırken q ve açık kullanıcı sıralamasının hidden alanlarla korunduğunu doğruluyorum.
  it("preserves search and explicit sorting in the filter form", () => {
    const markup = renderToStaticMarkup(
      <CatalogFilters facets={facets} view={{ page: 1, sort: "newest", hasExplicitSort: true, search: "inci kolye" }} />,
    );

    expect(markup).toContain('name="q" value="inci kolye"');
    expect(markup).toContain('name="sort" value="newest"');
  });

  // Burada koleksiyon landing sayfasının sabit koleksiyon ID'sini URL'ye taşımadan diğer iki filtreyi aynı katalog tasarımında sunduğunu doğruluyorum.
  it("keeps a classification filter fixed on its clean landing URL", () => {
    const markup = renderToStaticMarkup(
      <CatalogFilters
        facets={facets}
        view={{ page: 1, sort: "newest", collectionId }}
        urlOptions={{ basePath: "/collection/aksesuarlar", omitFilter: "collectionId" }}
      />,
    );

    expect(markup).toContain('action="/collection/aksesuarlar"');
    expect(markup).toContain("Tüm markalar");
    expect(markup).toContain("Tüm ürün türleri");
    expect(markup).not.toContain("Tüm koleksiyonlar");
    expect(markup).not.toContain(`value="${collectionId}"`);
  });
});
