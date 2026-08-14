import { describe, expect, it } from "vitest";

import { isProductPublicId, parseFavoritePage, parseFavoritePageSize } from "./request";

describe("favorite request validation", () => {
  // Burada yalnız canonical ürün public ID değerlerinin mutation yoluna geçebildiğini doğruluyorum.
  it("accepts canonical product ids and rejects malformed values", () => {
    expect(isProductPublicId("P00001")).toBe(true);
    expect(isProductPublicId("P1ABCD9Z")).toBe(true);
    expect(isProductPublicId("P00000")).toBe(false);
    expect(isProductPublicId("p00001")).toBe(false);
    expect(isProductPublicId("P00001/extra")).toBe(false);
  });

  // Burada geçersiz, çok değerli ve aşırı sayfa parametrelerini güvenli ilk sayfaya çektiğimi doğruluyorum.
  it("normalizes favorite pagination", () => {
    expect(parseFavoritePage(undefined)).toBe(1);
    expect(parseFavoritePage("2")).toBe(2);
    expect(parseFavoritePage(["3", "4"])).toBe(3);
    expect(parseFavoritePage("0")).toBe(1);
    expect(parseFavoritePage("10001")).toBe(1);
    expect(parseFavoritePage("invalid")).toBe(1);
    expect(parseFavoritePageSize(undefined)).toBe(20);
    expect(parseFavoritePageSize("12")).toBe(12);
    expect(parseFavoritePageSize(["24", "48"])).toBe(24);
    expect(parseFavoritePageSize("0")).toBe(20);
    expect(parseFavoritePageSize("101")).toBe(20);
  });
});
