import { describe, expect, it } from "vitest";
import { buildStockMovementListHref, parseStockMovementListQuery } from "./query";

describe("stock movement query", () => {
  // Burada belgelenen arama, tür, yön ve tarih filtrelerini API'ye uygun URL durumuna dönüştürdüğümü doğruluyorum.
  it("parses documented stock movement filters", () => {
    expect(parseStockMovementListQuery({
      search: "  Gümüş  ",
      direction: "2",
      type: "41",
      createdFrom: "2026-08-01",
      createdTo: "2026-08-05",
      pageSize: "50",
    })).toMatchObject({
      search: "Gümüş",
      direction: 2,
      type: 41,
      pageSize: 50,
      createdFromUtc: "2026-08-01T00:00:00.000Z",
      createdToUtc: "2026-08-05T23:59:59.999Z",
    });
  });

  // Burada geçersiz tarih aralığında API'ye tarih filtresi göndermediğimi doğruluyorum.
  it("keeps an invalid date range out of the API query", () => {
    const query = parseStockMovementListQuery({ createdFrom: "2026-08-05", createdTo: "2026-08-01" });
    expect(query.dateError).toBeTruthy();
    expect(query.createdFromUtc).toBeUndefined();
    expect(query.createdToUtc).toBeUndefined();
  });

  // Burada sayfalama bağlantısında aktif stok filtrelerini koruduğumu doğruluyorum.
  it("preserves filters in pagination links", () => {
    const query = parseStockMovementListQuery({ search: "SKU-1", direction: "1", type: "10", pageSize: "50" });
    expect(buildStockMovementListHref(query, 3)).toBe("/inventory/stock-movements?pageNumber=3&pageSize=50&search=SKU-1&direction=1&type=10");
  });
});
