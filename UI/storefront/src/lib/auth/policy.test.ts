import { describe, expect, it } from "vitest";

import { safeReturnTo } from "./policy";

describe("auth return target policy", () => {
  // Burada yalnızca Storefront içindeki göreli yol, query ve fragment birleşiminin korunabildiğini doğruluyorum.
  it("accepts a safe same-origin relative target", () => {
    expect(safeReturnTo("/products?page=2#results")).toBe("/products?page=2#results");
  });

  // Burada protokole göreli, mutlak, backslash içeren ve auth döngüsü oluşturan hedeflerin ana sayfaya kapandığını doğruluyorum.
  it.each(["https://evil.example", "//evil.example", "/\\evil.example", "/login", "/register?next=/cart", "\u0000/cart"])(
    "rejects unsafe target %s",
    (target) => expect(safeReturnTo(target)).toBe("/"),
  );
});
