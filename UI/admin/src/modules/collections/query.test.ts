import { describe, expect, it } from "vitest";
import { buildCollectionListHref, parseCollectionListQuery } from "./query";

describe("collection list query", () => {
  // Burada geçerli sayfalama değerlerinin URL'den okunduğunu doğruluyorum.
  it("parses documented pagination values", () => {
    expect(parseCollectionListQuery({ pageNumber: "3", pageSize: "50" })).toEqual({ pageNumber: 3, pageSize: 50 });
  });

  // Burada API sınırını aşan sayfa değerlerinin güvenli varsayılanlara döndüğünü doğruluyorum.
  it("rejects invalid pagination values", () => {
    expect(parseCollectionListQuery({ pageNumber: "0", pageSize: "101" })).toEqual({ pageNumber: 1, pageSize: 20 });
  });

  // Burada sayfa bağlantısının seçili sayfa boyutunu koruduğunu doğruluyorum.
  it("builds stable pagination hrefs", () => {
    expect(buildCollectionListHref({ pageNumber: 2, pageSize: 50 }, 4)).toBe("/collections?pageNumber=4&pageSize=50");
  });
});
