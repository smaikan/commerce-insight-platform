const TECHNICAL_VARIANT_VALUES = new Set(["default", "varsayılan"]);

// Burada yalnızca iki gerçek snapshot alanı birlikte bulunduğunda müşteriye varyant etiketi üretiyorum.
export function formatVariantLabel(
  variantName: string | null | undefined,
  variantValue: string | null | undefined,
): string | null {
  const name = visibleVariantPart(variantName);
  const value = visibleVariantPart(variantValue);

  if (!name || !value) return null;
  return `${name}: ${value}`;
}

// Burada API dışı eski veya teknik varsayılan değerlerin müşteri metnine sızmasını savunmacı biçimde engelliyorum.
function visibleVariantPart(value: string | null | undefined): string | null {
  const normalized = value?.trim();
  if (!normalized || TECHNICAL_VARIANT_VALUES.has(normalized.toLocaleLowerCase("tr-TR"))) return null;
  return normalized;
}
