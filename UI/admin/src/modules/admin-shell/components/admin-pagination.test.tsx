import { renderToStaticMarkup } from "react-dom/server";
import { describe, expect, it } from "vitest";

import { AdminPagination } from "./admin-pagination";

// Burada sayfalama gezinmesinin görsel okuma sırasını ve doğrudan atlama formunun filtreleri koruduğunu doğruluyorum.
describe("AdminPagination", () => {
  it("renders navigation before the separately labelled jump form", () => {
    const html = renderToStaticMarkup(
      <AdminPagination
        action="/products"
        ariaLabel="Ürün listesi sayfalama"
        buildHref={(pageNumber) => `/products?page=${pageNumber}`}
        hiddenFields={[{ name: "search", value: "uzun ürün" }]}
        itemLabel="ürün"
        pageNumber={2}
        pageParam="page"
        pageSize={20}
        totalCount={84}
        totalPages={5}
      />,
    );

    expect(html.indexOf("Önceki")).toBeLessThan(html.indexOf("Sayfa 2 / 5"));
    expect(html.indexOf("Sayfa 2 / 5")).toBeLessThan(html.indexOf("Sonraki"));
    expect(html.indexOf("Sonraki")).toBeLessThan(html.indexOf("Sayfaya git"));
    expect(html).toContain('name="search" value="uzun ürün"');
    expect(html).toContain('name="page"');
    expect(html).toContain('max="5"');
  });

  // Burada tek sayfalık listelerde gereksiz gezinme ve doğrudan atlama kontrollerinin görünmediğini doğruluyorum.
  it("keeps only the result summary when one page is enough", () => {
    const html = renderToStaticMarkup(
      <AdminPagination
        action="/brands"
        ariaLabel="Marka listesi sayfalama"
        buildHref={(pageNumber) => `/brands?pageNumber=${pageNumber}`}
        itemLabel="marka"
        pageNumber={1}
        pageSize={20}
        totalCount={7}
        totalPages={1}
      />,
    );

    expect(html).toContain("1-7");
    expect(html).not.toContain("Sayfaya git");
    expect(html).not.toContain("Önceki");
  });
});
