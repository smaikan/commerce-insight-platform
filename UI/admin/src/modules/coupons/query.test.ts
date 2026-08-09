import { describe, expect, it } from "vitest";
import { buildCouponListHref, parseCouponListQuery } from "./query";

describe("coupon list query", () => {
  // Burada yalnız desteklenen aktiflik filtresinin güvenle çözüldüğünü doğruluyorum.
  it("parses documented pagination and activity filters", () => {
    expect(parseCouponListQuery({ pageNumber: "3", pageSize: "50", isActive: "false" })).toEqual({ pageNumber: 3, pageSize: 50, isActive: false });
  });

  // Burada bozuk veya desteklenmeyen URL değerlerinin güvenli varsayılanlara döndüğünü doğruluyorum.
  it("falls back for invalid query values", () => {
    expect(parseCouponListQuery({ pageNumber: "0", pageSize: "30", isActive: "yes" })).toEqual({ pageNumber: 1, pageSize: 20, isActive: undefined });
  });

  // Burada filtrelerin sayfa bağlantılarında korunduğunu doğruluyorum.
  it("keeps documented filters in pagination links", () => {
    expect(buildCouponListHref({ pageNumber: 2, pageSize: 50, isActive: true }, 4)).toBe("/coupons?pageNumber=4&pageSize=50&isActive=true");
  });
});
