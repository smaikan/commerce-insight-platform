import type { ProductVariant } from "./types";
import { splitComposite, type VariantCombination } from "./variant-combinations";

// Burada yalnız ad/değer şeması canonical kombinasyondan farklı olan mevcut kaydı normalizasyon adayı sayıyorum.
export function variantNeedsCanonicalIdentity(
  variant: Pick<ProductVariant, "name" | "value">,
  combination: Pick<VariantCombination, "name" | "value">,
): boolean {
  return variant.name !== combination.name || variant.value !== combination.value;
}

// Burada tam şemaları doğrudan, eksik legacy şemaları ise yalnız tek bir olası kombinasyon varsa güvenle eşliyorum.
export function variantsByCombination(
  variants: readonly ProductVariant[],
  combinations: readonly VariantCombination[],
): Record<string, ProductVariant[]> {
  const grouped: Record<string, ProductVariant[]> = {};
  variants.forEach((variant) => {
    const variantNames = splitComposite(variant.name);
    const variantValues = splitComposite(variant.value);
    if (variantNames.length !== variantValues.length) return;

    const candidates = combinations.filter((item) => {
      const combinationNames = splitComposite(item.name);
      const combinationValues = splitComposite(item.value);
      return variantNames.every((name, index) => {
        const combinationIndex = combinationNames.indexOf(name);
        return combinationIndex >= 0 && combinationValues[combinationIndex] === variantValues[index];
      });
    });
    const exact = candidates.find((item) => {
      const combinationNames = splitComposite(item.name);
      return combinationNames.length === variantNames.length
        && combinationNames.every((name, index) => name === variantNames[index]);
    });
    const combination = exact || (candidates.length === 1 ? candidates[0] : undefined);
    if (!combination) return;
    grouped[combination.key] = [...(grouped[combination.key] || []), variant];
  });
  return grouped;
}

// Burada başarılı kayıt sonrası API'den değişen varyant listesinin client editörünü yeni kimliklerle kurmasını sağlıyorum.
export function editableVariantRevision(variants: readonly ProductVariant[]): string {
  return JSON.stringify(variants.map((variant) => [
    variant.id,
    variant.name,
    variant.value,
    variant.sku,
    variant.barcode,
    variant.material,
    variant.price,
    variant.compareAtPrice,
    variant.stock,
    variant.isActive,
    variant.concurrencyToken,
  ]));
}
