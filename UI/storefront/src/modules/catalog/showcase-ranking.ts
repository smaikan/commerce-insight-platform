export type ProductCountedShowcaseItem = {
  id: string;
  name: string;
  productCount: number;
};

// Burada ana sayfa vitrinlerinde gerçek ürün yoğunluğunu belirleyici, ad ve kimliği ise kararlı eşitlik sırası olarak kullanıyorum.
export function selectMostPopulated<T extends ProductCountedShowcaseItem>(
  items: readonly T[],
  limit: number,
): T[] {
  if (!Number.isSafeInteger(limit) || limit <= 0) return [];

  return [...items]
    .filter((item) => item.productCount > 0)
    .sort((left, right) =>
      right.productCount - left.productCount
      || left.name.localeCompare(right.name, "tr")
      || left.id.localeCompare(right.id))
    .slice(0, limit);
}
