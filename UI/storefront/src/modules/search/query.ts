const MAX_SEARCH_LENGTH = 100;

// Burada kullanıcı sorgusunu backend ile aynı trim ve ardışık boşluk kuralına indiriyorum.
export function normalizeSearchQuery(value: string): string {
  return value.trim().replace(/\s+/g, " ");
}

// Burada yalnız belgeli 2-100 karakter aralığındaki sorguların API sınırına ulaşmasına izin veriyorum.
export function isSearchQueryValid(value: string): boolean {
  const normalized = normalizeSearchQuery(value);
  return normalized.length >= 2 && normalized.length <= MAX_SEARCH_LENGTH;
}

// Burada modal ile katalog arasında tek ve güvenli paylaşılabilir arama URL'si üretiyorum.
export function searchResultsHref(query: string): string {
  return `/products?q=${encodeURIComponent(normalizeSearchQuery(query))}`;
}
