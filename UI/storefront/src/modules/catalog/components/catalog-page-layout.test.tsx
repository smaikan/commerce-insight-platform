import { renderToStaticMarkup } from "react-dom/server";
import { describe, expect, it } from "vitest";

import { CatalogPageLayout } from "./catalog-page-layout";

// Burada katalog başlığını ürün gerektirmeyen boş API sonucu üzerinden doğruluyorum.
const emptyProducts = {
  items: [],
  pageNumber: 1,
  pageSize: 20,
  totalCount: 0,
  totalPages: 0,
  hasPreviousPage: false,
  hasNextPage: false,
};

const emptyFacets = { brands: [], collections: [], productTypes: [] };

describe("catalog page layout", () => {
  // Burada ana katalogda eski tanıtım etiketi ve doldurma metni olmadan doğrudan ürün başlığının gösterildiğini doğruluyorum.
  it("renders a restrained catalog heading without promotional filler", () => {
    const html = renderToStaticMarkup(
      <CatalogPageLayout
        title="Tüm ürünler"
        products={emptyProducts}
        facets={emptyFacets}
        view={{ page: 1, sort: "newest" }}
        emptyDescription="Henüz ürün yok."
      />,
    );

    expect(html).toContain(">Tüm ürünler</h1>");
    expect(html).not.toContain(">Mağaza</p>");
    expect(html).not.toContain("Güncel ürünleri sade bir katalogda inceleyin");
  });

  // Burada sınıflandırma sayfasının gerçek tür ve açıklamayı koruduğunu doğruluyorum.
  it("renders an authoritative classification label and description", () => {
    const html = renderToStaticMarkup(
      <CatalogPageLayout
        eyebrow="Marka"
        title="Örnek Marka"
        description="Markanın API tarafından sağlanan açıklaması."
        products={emptyProducts}
        facets={emptyFacets}
        view={{ page: 1, sort: "newest" }}
        emptyDescription="Henüz ürün yok."
      />,
    );

    expect(html).toContain(">Marka</p>");
    expect(html).toContain(">Örnek Marka</h1>");
    expect(html).toContain("Markanın API tarafından sağlanan açıklaması.");
  });
});
