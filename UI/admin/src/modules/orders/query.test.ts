import { describe, expect, it } from "vitest";
import { buildOrderListHref, hasOrderFilters, parseOrderListQuery } from "./query";

describe("order list query", () => {
  // Burada sipariş listesinin backend varsayılanlarıyla uyumlu güvenli başlangıç sorgusunu doğruluyorum.
  it("uses documented defaults", () => {
    const query = parseOrderListQuery({ status: "" });
    expect(query).toMatchObject({ pageNumber: 1, pageSize: 20 });
    expect(query.status).toBeUndefined();
  });

  // Burada durum ve tarih filtrelerinin numeric enum ile UTC gün sınırlarına dönüştüğünü doğruluyorum.
  it("parses status and UTC date boundaries", () => {
    const query = parseOrderListQuery({
      status: "4",
      createdFrom: "2026-08-01",
      createdTo: "2026-08-03",
      pageSize: "50",
    });

    expect(query).toMatchObject({
      status: 4,
      pageSize: 50,
      createdFromUtc: "2026-08-01T00:00:00.000Z",
      createdToUtc: "2026-08-03T23:59:59.999Z",
    });
    expect(hasOrderFilters(query)).toBe(true);
  });

  // Burada ters tarih aralığının API'ye hatalı filtre göndermeden kullanıcı hatasına dönüştüğünü doğruluyorum.
  it("keeps an invalid date range out of the API query", () => {
    const query = parseOrderListQuery({ createdFrom: "2026-08-03", createdTo: "2026-08-01" });

    expect(query.dateError).toBeDefined();
    expect(query.createdFromUtc).toBeUndefined();
    expect(query.createdToUtc).toBeUndefined();
  });

  // Burada sayfalama bağlantısının filtreleri koruyup varsayılan değerleri URL'de çoğaltmadığını doğruluyorum.
  it("builds a filtered pagination href", () => {
    const query = parseOrderListQuery({ status: "2", createdFrom: "2026-08-01" });
    expect(buildOrderListHref(query, 3)).toBe("/orders?pageNumber=3&status=2&createdFrom=2026-08-01");
  });
});
