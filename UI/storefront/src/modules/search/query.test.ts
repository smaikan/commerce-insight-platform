import { describe, expect, it } from "vitest";

import { isSearchQueryValid, normalizeSearchQuery, searchResultsHref } from "./query";

describe("search query", () => {
  // Burada boşluk normalizasyonu ve API'nin 2-100 karakter sınırını aynı noktada doğruluyorum.
  it("normalizes and validates the documented search range", () => {
    expect(normalizeSearchQuery("  inci   kolye  ")).toBe("inci kolye");
    expect(isSearchQueryValid("i")).toBe(false);
    expect(isSearchQueryValid("in")).toBe(true);
    expect(isSearchQueryValid("a".repeat(101))).toBe(false);
  });

  // Burada tam sonuç URL'sinin normalize sorguyu güvenli q parametresinde koruduğunu doğruluyorum.
  it("builds a safe catalog search URL", () => {
    expect(searchResultsHref(" gözlük   zinciri ")).toBe("/products?q=g%C3%B6zl%C3%BCk%20zinciri");
  });
});
