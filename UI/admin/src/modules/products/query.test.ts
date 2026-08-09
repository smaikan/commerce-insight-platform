import { describe, expect, it } from "vitest";
import { buildProductListHref, parseProductListQuery } from "./query";

describe("product list query", () => {
  // Burada sayfa boyutunun ürün listesinde varsayılan olarak 20 kaldığını doğruluyorum.
  it("uses documented paging and newest-first defaults", () => {
    expect(parseProductListQuery({})).toMatchObject({
      pageNumber: 1,
      pageSize: 20,
      sortBy: 2,
      descending: true,
    });
  });

  // Burada yalnız backend'in desteklediği filtre ve sıralama değerlerinin URL'den kabul edildiğini doğruluyorum.
  it("parses supported filters and clamps invalid page size", () => {
    expect(
      parseProductListQuery({
        search: "  Küpe ",
        status: "1",
        isFeatured: "true",
        sort: "title-asc",
        page: "3",
        pageSize: "500",
      }),
    ).toMatchObject({
      search: "Küpe",
      status: 1,
      isFeatured: true,
      sortBy: 1,
      descending: false,
      pageNumber: 3,
      pageSize: 20,
    });
  });

  // Burada sayfalama bağlantısının etkin ürün filtrelerini kaybetmediğini doğruluyorum.
  it("preserves filter state in pagination hrefs", () => {
    const brandId = "90889aa5-e32a-48d9-a16a-90d663def971";
    const query = parseProductListQuery({ search: "kolye", brandId, pageSize: "50", sort: "created-desc" });
    const href = buildProductListHref(query, 2);

    expect(href).toContain("page=2");
    expect(href).toContain("pageSize=50");
    expect(href).toContain("search=kolye");
    expect(href).toContain(`brandId=${brandId}`);
    expect(href).toContain("sort=created-desc");
  });
});
