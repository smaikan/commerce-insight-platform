import { describe, expect, it } from "vitest";
import { buildBrandListHref, parseBrandListQuery } from "./query";

describe("brand list query", () => {
  // Burada geçerli marka sayfalama değerlerinin URL'den okunduğunu doğruluyorum.
  it("parses documented pagination values", () => {
    expect(parseBrandListQuery({ pageNumber: "3", pageSize: "50" })).toEqual({ pageNumber: 3, pageSize: 50 });
  });

  // Burada API sınırını aşan değerlerin güvenli varsayılanlara döndüğünü doğruluyorum.
  it("rejects invalid pagination values", () => {
    expect(parseBrandListQuery({ pageNumber: "0", pageSize: "101" })).toEqual({ pageNumber: 1, pageSize: 20 });
  });

  // Burada sayfa bağlantısının seçili sayfa boyutunu koruduğunu doğruluyorum.
  it("builds stable pagination hrefs", () => {
    expect(buildBrandListHref({ pageNumber: 2, pageSize: 50 }, 4)).toBe("/brands?pageNumber=4&pageSize=50");
    expect(buildBrandListHref({ pageNumber: 1, pageSize: 20 })).toBe("/brands");
  });
});
