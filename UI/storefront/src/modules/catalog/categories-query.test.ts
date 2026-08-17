import { describe, expect, it } from "vitest";

import {
  categoriesHref,
  categoriesSearchParamsNeedRedirect,
  parseCategoriesView,
} from "./categories-query";

describe("categories URL pagination", () => {
  // Burada geçerli URL değerlerinin API PageNumber ve PageSize değerlerine karşılık geldiğini doğruluyorum.
  it("parses valid page and page size values", () => {
    const view = parseCategoriesView({ page: "3", pageSize: "100" });

    expect(view).toEqual({ page: 3, pageSize: 100 });
    expect(categoriesSearchParamsNeedRedirect({ page: "3", pageSize: "100" }, view)).toBe(false);
  });

  // Burada geçersiz, yinelenen ve gereksiz parametrelerin temiz URL gerektirdiğini doğruluyorum.
  it.each([
    { page: "0" },
    { page: "-2" },
    { page: "abc" },
    { pageSize: "0" },
    { pageSize: "101" },
    { pageSize: "20" },
    { page: ["2", "3"] },
  ])("normalizes invalid or redundant params: %o", (params) => {
    const view = parseCategoriesView(params);

    expect(categoriesSearchParamsNeedRedirect(params, view)).toBe(true);
  });

  // Burada sayfalama bağlantılarının varsayılanları gizleyip özel sayfa boyutunu koruduğunu doğruluyorum.
  it("builds canonical pagination links", () => {
    expect(categoriesHref({ page: 1, pageSize: 20 })).toBe("/categories");
    expect(categoriesHref({ page: 4, pageSize: 20 })).toBe("/categories?page=4");
    expect(categoriesHref({ page: 1, pageSize: 40 })).toBe("/categories?pageSize=40");
    expect(categoriesHref({ page: 3, pageSize: 40 })).toBe("/categories?page=3&pageSize=40");
  });
});
