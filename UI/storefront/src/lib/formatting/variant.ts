const TECHNICAL_VARIANT_VALUES = new Set(["default", "varsayılan"]);

export type VariantAttribute = {
  name: string;
  value: string;
};

// Burada API'nin "Renk / Beden" ve "Kırmızı / L" biçimindeki birleşik snapshot alanlarını
// aynı sıradaki ad ve değerleri eşleştirerek müşteriye anlamlı seçenekler halinde sunuyorum.
export function parseVariantAttributes(
  variantName: string | null | undefined,
  variantValue: string | null | undefined,
): VariantAttribute[] {
  const name = visibleVariantPart(variantName);
  const value = visibleVariantPart(variantValue);

  if (!name || !value) return [];

  const names = splitCompositePart(name);
  const values = splitCompositePart(value);

  if (names.length > 1 && names.length === values.length) {
    return names.map((item, index) => ({ name: item, value: values[index] }));
  }

  return [{ name, value }];
}

// Burada yalnızca iki gerçek snapshot alanı birlikte bulunduğunda müşteriye varyant etiketi üretiyorum.
export function formatVariantLabel(
  variantName: string | null | undefined,
  variantValue: string | null | undefined,
): string | null {
  const attributes = parseVariantAttributes(variantName, variantValue);

  if (attributes.length === 0) return null;
  return attributes.map(({ name, value }) => `${name}: ${value}`).join(" · ");
}

function splitCompositePart(value: string): string[] {
  return value.split("/").map((part) => part.trim()).filter(Boolean);
}

// Burada API dışı eski veya teknik varsayılan değerlerin müşteri metnine sızmasını savunmacı biçimde engelliyorum.
function visibleVariantPart(value: string | null | undefined): string | null {
  const normalized = value?.trim();
  if (!normalized || TECHNICAL_VARIANT_VALUES.has(normalized.toLocaleLowerCase("tr-TR"))) return null;
  return normalized;
}
