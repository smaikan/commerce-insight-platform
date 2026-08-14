import { describe, expect, it } from "vitest";

import {
  collectionsHref,
  collectionsSearchParamsNeedRedirect,
  parseCollectionsView,
} from "./collections-query";

describe("collections URL pagination", () => {
  // Burada geçerli URL değerlerinin API'nin PageNumber ve PageSize değerlerine karşılık geldiğini doğruluyorum.
  it("parses valid page and page size values", () => {
    const view = parseCollectionsView({ page: "3", pageSize: "100" });

    expect(view).toEqual({ page: 3, pageSize: 100 });
    expect(collectionsSearchParamsNeedRedirect({ page: "3", pageSize: "100" }, view)).toBe(false);
  });

  // Burada geçersiz ve API sınırını aşan değerlerin güvenli varsayılanlara dönüp temiz URL gerektirdiğini doğruluyorum.
  it.each([
    { page: "0" },
    { page: "-2" },
    { page: "abc" },
    { pageSize: "0" },
    { pageSize: "101" },
    { pageSize: "20" },
    { page: ["2", "3"] },
  ])("normalizes invalid or redundant params: %o", (params) => {
    const view = parseCollectionsView(params);

    expect(collectionsSearchParamsNeedRedirect(params, view)).toBe(true);
  });

  // Burada sayfalama bağlantılarının varsayılanları gizleyip seçilmiş özel sayfa boyutunu koruduğunu doğruluyorum.
  it("builds canonical pagination links", () => {
    expect(collectionsHref({ page: 1, pageSize: 20 })).toBe("/collections");
    expect(collectionsHref({ page: 4, pageSize: 20 })).toBe("/collections?page=4");
    expect(collectionsHref({ page: 1, pageSize: 40 })).toBe("/collections?pageSize=40");
    expect(collectionsHref({ page: 3, pageSize: 40 })).toBe("/collections?page=3&pageSize=40");
  });
});
