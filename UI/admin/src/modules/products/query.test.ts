import { describe, expect, it } from "vitest";
import { buildProductListHref, buildRemoveProductFilterHref, parseProductListQuery } from "./query";

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
    const collectionId = "11111111-1111-4111-8111-111111111111";
    const tagId = "22222222-2222-4222-8222-222222222222";
    expect(
      parseProductListQuery({
        search: "  Küpe ",
        collectionId,
        tagId,
        status: "1",
        isFeatured: "true",
        sort: "title-asc",
        page: "3",
        pageSize: "500",
      }),
    ).toMatchObject({
      search: "Küpe",
      collectionId,
      tagId,
      status: 1,
      isFeatured: true,
      sortBy: 1,
      descending: false,
      pageNumber: 3,
      pageSize: 20,
    });
  });

  // Burada filtre formundaki boş durum değerinin Taslak enumuna dönüşmeden Tüm durumlar olarak kaldığını doğruluyorum.
  it("keeps an unchanged status filter empty", () => {
    expect(parseProductListQuery({ search: "kolye", status: "" })).toMatchObject({
      search: "kolye",
      status: undefined,
    });
    expect(parseProductListQuery({ status: "   " }).status).toBeUndefined();
  });

  // Burada sayfalama bağlantısının etkin ürün filtrelerini kaybetmediğini doğruluyorum.
  it("preserves filter state in pagination hrefs", () => {
    const brandId = "90889aa5-e32a-48d9-a16a-90d663def971";
    const collectionId = "11111111-1111-4111-8111-111111111111";
    const tagId = "22222222-2222-4222-8222-222222222222";
    const query = parseProductListQuery({
      search: "kolye",
      brandId,
      collectionId,
      tagId,
      pageSize: "50",
      sort: "created-desc",
    });
    const href = buildProductListHref(query, 2);

    expect(href).toContain("page=2");
    expect(href).toContain("pageSize=50");
    expect(href).toContain("search=kolye");
    expect(href).toContain(`brandId=${brandId}`);
    expect(href).toContain(`collectionId=${collectionId}`);
    expect(href).toContain(`tagId=${tagId}`);
    expect(href).toContain("sort=created-desc");
  });

  // Burada tekil bir filtrenin kaldırılmasıyla üretilen URL'in yalnız o filtreyi temizleyip sayfa 1'e döndüğünü doğruluyorum.
  it("builds correct URL when removing a single filter", () => {
    const query = parseProductListQuery({
      search: "yüzük",
      collectionId: "11111111-1111-4111-8111-111111111111",
      tagId: "22222222-2222-4222-8222-222222222222",
      page: "4",
    });

    const withoutCollection = buildRemoveProductFilterHref(query, "collectionId");
    expect(withoutCollection).toContain("search=y%C3%BCz%C3%BCk");
    expect(withoutCollection).toContain("tagId=22222222-2222-4222-8222-222222222222");
    expect(withoutCollection).not.toContain("collectionId=");
    expect(withoutCollection).toContain("page=1");
  });
});
