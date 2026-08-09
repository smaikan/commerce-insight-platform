import { describe, expect, it } from "vitest";
import { buildCustomerListHref, hasCustomerFilters, parseCustomerListQuery } from "./query";

describe("customer list query", () => {
  // Burada müşteri listesinin backend varsayılanlarıyla uyumlu güvenli başlangıç sorgusunu doğruluyorum.
  it("uses documented defaults", () => {
    const query = parseCustomerListQuery({});
    expect(query).toMatchObject({ pageNumber: 1, pageSize: 20 });
    expect(query.search).toBeUndefined();
    expect(query.role).toBe(1);
    expect(query.status).toBeUndefined();
  });

  // Burada arama, rol ve durum filtrelerinin belgelenmiş enum değerlerine dönüştüğünü doğruluyorum.
  it("parses search and status while fixing role to customer", () => {
    const query = parseCustomerListQuery({ search: "ahmet", role: "2", status: "2" });
    expect(query).toMatchObject({ search: "ahmet", role: 1, status: 2 });
    expect(hasCustomerFilters(query)).toBe(true);
  });

  // Burada belgelenmiş dışındaki enum değerlerinin filtre olarak kabul edilmediğini doğruluyorum.
  it("rejects out-of-range status", () => {
    const query = parseCustomerListQuery({ role: "99", status: "99" });
    expect(query.role).toBe(1);
    expect(query.status).toBeUndefined();
  });

  // Burada boş arama stringinin filtre olarak kabul edilmediğini doğruluyorum.
  it("treats empty search as no filter", () => {
    const query = parseCustomerListQuery({ search: "   " });
    expect(query.search).toBeUndefined();
    expect(hasCustomerFilters(query)).toBe(false);
  });

  // Burada sayfalama bağlantısının filtreleri koruyup varsayılan değerleri URL'de çoğaltmadığını doğruluyorum.
  it("builds a filtered pagination href", () => {
    const query = parseCustomerListQuery({ search: "zeynep", status: "1" });
    expect(buildCustomerListHref(query, 3)).toBe("/customers?pageNumber=3&search=zeynep&status=1");
  });

  // Burada filtre yoksa temiz /customers href'i üretildiğini doğruluyorum.
  it("builds clean href with no filters", () => {
    const query = parseCustomerListQuery({});
    expect(buildCustomerListHref(query, 1)).toBe("/customers");
  });
});
