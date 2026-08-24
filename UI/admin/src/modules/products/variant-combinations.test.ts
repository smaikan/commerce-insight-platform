import { describe, expect, it } from "vitest";
import { buildVariantCombinations, groupsFromVariants, type VariantOptionGroupDraft } from "./variant-combinations";

// Burada kombinasyon testleri için okunabilir seçenek grupları oluşturuyorum.
function group(key: string, name: string, values: string[]): VariantOptionGroupDraft {
  return {
    key,
    name,
    persisted: false,
    values: values.map((value, index) => ({ key: `${key}-${index}`, value, persisted: false })),
  };
}

describe("variant combinations", () => {
  // Burada iki seçenek grubunun tüm çapraz kombinasyonları ürettiğini doğruluyorum.
  it("builds the cartesian product for two option groups", () => {
    const combinations = buildVariantCombinations([
      group("color", "Renk", ["Siyah", "Beyaz"]),
      group("gender", "Cins", ["Kadın", "Erkek"]),
    ]);

    expect(combinations.map(({ name, value }) => ({ name, value }))).toEqual([
      { name: "Renk / Cins", value: "Siyah / Kadın" },
      { name: "Renk / Cins", value: "Siyah / Erkek" },
      { name: "Renk / Cins", value: "Beyaz / Kadın" },
      { name: "Renk / Cins", value: "Beyaz / Erkek" },
    ]);
  });

  // Burada backend'den dönen birleşik varyantların seçenek gruplarına geri ayrıldığını doğruluyorum.
  it("reconstructs option groups from composite variants", () => {
    const groups = groupsFromVariants([
      { name: "Renk / Cins", value: "Siyah / Kadın" },
      { name: "Renk / Cins", value: "Siyah / Erkek" },
      { name: "Renk / Cins", value: "Beyaz / Kadın" },
    ]);

    expect(groups.map((item) => ({ name: item.name, values: item.values.map((value) => value.value) }))).toEqual([
      { name: "Renk", values: ["Siyah", "Beyaz"] },
      { name: "Cins", values: ["Kadın", "Erkek"] },
    ]);
  });

  it("uses the richest schema and matches legacy values by option name", () => {
    const groups = groupsFromVariants([
      { name: "Beden", value: "L" },
      { name: "Beden", value: "M" },
      { name: "Renk / Beden", value: "Kırmızı / L" },
    ]);

    expect(groups.map((item) => ({ name: item.name, values: item.values.map((value) => value.value) }))).toEqual([
      { name: "Renk", values: ["Kırmızı"] },
      { name: "Beden", values: ["L", "M"] },
    ]);
  });
});
