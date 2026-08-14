const PRODUCT_PUBLIC_ID_PATTERN = /^P[0-9A-Z]{5,7}$/;

// Burada BFF yoluna gelen ürün kimliğini backend public ID biçimiyle sınırlandırıyorum.
export function isProductPublicId(value: string): boolean {
  return PRODUCT_PUBLIC_ID_PATTERN.test(value) && !/^P0+$/.test(value);
}

// Burada favori sayfasının URL parametresini API'nin güvenli sayfa aralığına indiriyorum.
export function parseFavoritePage(value: string | string[] | undefined): number {
  const candidate = Array.isArray(value) ? value[0] : value;
  const page = Number.parseInt(candidate || "1", 10);
  return Number.isSafeInteger(page) && page > 0 && page <= 10_000 ? page : 1;
}

// Burada favori sayfa boyutunu API doğrulama aralığında tutup URL üzerinden korunabilir hale getiriyorum.
export function parseFavoritePageSize(value: string | string[] | undefined): number {
  const candidate = Array.isArray(value) ? value[0] : value;
  const pageSize = Number.parseInt(candidate || "20", 10);
  return Number.isSafeInteger(pageSize) && pageSize > 0 && pageSize <= 100 ? pageSize : 20;
}
