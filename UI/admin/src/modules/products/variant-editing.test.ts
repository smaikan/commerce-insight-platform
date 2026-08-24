import { describe, expect, it } from "vitest";
import { editableVariantRevision, variantNeedsCanonicalIdentity, variantsByCombination } from "./variant-editing";
import { buildVariantCombinations, groupsFromVariants } from "./variant-combinations";
import type { ProductVariant } from "./types";

function variant(index: number, value: string, name = "Renk"): ProductVariant {
  return {
    id: `11111111-1111-4111-8111-11111111111${index}`,
    productId: "P00004",
    name,
    value,
    variantOptionNameId: null,
    variantOptionValueId: null,
    sku: `SKU-${index}`,
    barcode: null,
    material: null,
    price: 100 + index,
    netPrice: 90 + index,
    compareAtPrice: null,
    stock: index,
    addToCartCount: 0,
    purchaseCount: 0,
    isActive: true,
    concurrencyToken: `aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa${index}`,
  };
}

describe("variant editing", () => {
  it("preserves every persisted id under cartesian combinations when logical values overlap", () => {
    const variants = [
      variant(0, "Siyah"),
      variant(1, "Beyaz"),
      variant(2, "Mavi"),
      variant(3, "Kırmızı"),
      variant(4, "Siyah"),
      variant(5, "Beyaz"),
      variant(6, "Mavi"),
    ];

    const combinations = buildVariantCombinations(groupsFromVariants(variants));
    const grouped = variantsByCombination(variants, combinations);
    const groupedVariants = combinations.flatMap((combination) => grouped[combination.key] || []);

    expect(combinations).toHaveLength(4);
    expect(groupedVariants).toHaveLength(7);
    expect(groupedVariants.map((item) => item.id).sort()).toEqual(variants.map((item) => item.id).sort());
  });

  it("changes the revision when the API returns a newly persisted variant id", () => {
    const before = [variant(0, "Siyah")];
    const after = [...before, variant(1, "Beyaz")];

    expect(editableVariantRevision(after)).not.toBe(editableVariantRevision(before));
  });

  it("changes the revision when a bulk response rotates the concurrency token", () => {
    const before = variant(0, "Siyah");
    const after = { ...before, concurrencyToken: "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb" };

    expect(editableVariantRevision([after])).not.toBe(editableVariantRevision([before]));
  });

  it("keeps composite and uniquely compatible legacy records in the reconstructed combinations", () => {
    const variants = [
      variant(0, "L", "Beden"),
      variant(1, "M", "Beden"),
      variant(2, "Kırmızı / L", "Renk / Beden"),
    ];
    const combinations = buildVariantCombinations(groupsFromVariants(variants));
    const grouped = variantsByCombination(variants, combinations);

    expect(combinations.map(({ name, value }) => ({ name, value }))).toEqual([
      { name: "Renk / Beden", value: "Kırmızı / L" },
      { name: "Renk / Beden", value: "Kırmızı / M" },
    ]);
    expect(grouped[combinations[0].key]).toHaveLength(2);
    expect(grouped[combinations[1].key]).toHaveLength(1);
    expect(Object.values(grouped).flat().map((item) => item.id).sort()).toEqual(
      variants.map((item) => item.id).sort(),
    );
  });

  it("marks only legacy identities for canonical schema normalization", () => {
    const canonical = { name: "Renk / Beden", value: "Kırmızı / L" };

    expect(variantNeedsCanonicalIdentity({ name: "Beden", value: "L" }, canonical)).toBe(true);
    expect(variantNeedsCanonicalIdentity(canonical, canonical)).toBe(false);
  });
});
