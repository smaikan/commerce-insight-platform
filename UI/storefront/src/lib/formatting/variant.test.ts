import { describe, expect, it } from "vitest";

import { formatVariantLabel } from "@/lib/formatting/variant";

describe("variant label formatting", () => {
  // Burada iki snapshot alanı dolu olduğunda müşterinin tam seçimini birlikte gösterdiğimi doğruluyorum.
  it("formats the variant name and value together", () => {
    expect(formatVariantLabel("Renk", "Pudra")).toBe("Renk: Pudra");
  });

  // Burada eski ve varyantsız kayıtlarda boş ayraç, undefined veya tek başına teknik bilgi üretmediğimi doğruluyorum.
  it.each([
    [null, null],
    ["Renk", null],
    [null, "Pudra"],
    [undefined, undefined],
    ["Default", "Default"],
    ["Varsayılan", "Varsayılan"],
  ])("hides incomplete or technical values (%s, %s)", (name, value) => {
    expect(formatVariantLabel(name, value)).toBeNull();
  });
});
