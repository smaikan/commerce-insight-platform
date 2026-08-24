import { describe, expect, it } from "vitest";

import { formatVariantLabel, parseVariantAttributes } from "@/lib/formatting/variant";

describe("variant label formatting", () => {
  // Burada iki snapshot alanı dolu olduğunda müşterinin tam seçimini birlikte gösterdiğimi doğruluyorum.
  it("formats the variant name and value together", () => {
    expect(formatVariantLabel("Renk", "Pudra")).toBe("Renk: Pudra");
  });

  // Burada birleşik varyant adları ile değerlerinin sıraya göre çaprazlanıp ayrı niteliklere dönüştüğünü doğruluyorum.
  it("pairs composite variant names and values by their positions", () => {
    expect(parseVariantAttributes("Renk / Beden", "Kırmızı / L")).toEqual([
      { name: "Renk", value: "Kırmızı" },
      { name: "Beden", value: "L" },
    ]);
    expect(formatVariantLabel("Renk / Beden", "Kırmızı / L")).toBe(
      "Renk: Kırmızı · Beden: L",
    );
  });

  // Burada ad ve değer parça sayıları uyuşmadığında herhangi bir parçayı kaybetmeden mevcut snapshot'ı koruyorum.
  it("keeps mismatched composite snapshots intact", () => {
    expect(parseVariantAttributes("Model", "Regular / Slim")).toEqual([
      { name: "Model", value: "Regular / Slim" },
    ]);
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
