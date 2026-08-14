export const COLLECTIONS_DEFAULT_PAGE_SIZE = 20;
export const COLLECTIONS_MAX_PAGE_SIZE = 100;

export type CollectionsSearchParams = Record<string, string | string[] | undefined>;

export type CollectionsView = {
  page: number;
  pageSize: number;
};

// Burada URL sayfalamasını API'nin pozitif PageNumber ve 1..100 PageSize sınırlarına indiriyorum.
export function parseCollectionsView(searchParams: CollectionsSearchParams): CollectionsView {
  return {
    page: positiveInteger(firstValue(searchParams.page)) ?? 1,
    pageSize: boundedPageSize(firstValue(searchParams.pageSize)) ?? COLLECTIONS_DEFAULT_PAGE_SIZE,
  };
}

// Burada bozuk, çok değerli veya gereksiz varsayılan parametreleri tek temiz koleksiyon URL'sine yönlendirmek için saptıyorum.
export function collectionsSearchParamsNeedRedirect(
  searchParams: CollectionsSearchParams,
  view: CollectionsView,
): boolean {
  return !matchesSingleValue(searchParams.page, view.page > 1 ? String(view.page) : undefined)
    || !matchesSingleValue(
      searchParams.pageSize,
      view.pageSize !== COLLECTIONS_DEFAULT_PAGE_SIZE ? String(view.pageSize) : undefined,
    );
}

// Burada sayfalama bağlantılarında varsayılan değerleri URL'den çıkarıp seçilmiş geçerli sayfa boyutunu koruyorum.
export function collectionsHref(view: CollectionsView): string {
  const query = new URLSearchParams();
  if (view.page > 1) query.set("page", String(view.page));
  if (view.pageSize !== COLLECTIONS_DEFAULT_PAGE_SIZE) query.set("pageSize", String(view.pageSize));
  const suffix = query.toString();
  return suffix ? `/collections?${suffix}` : "/collections";
}

// Burada yalnız tek bir URL değerinin beklenen temiz karşılıkla birebir eşleşmesini kabul ediyorum.
function matchesSingleValue(value: string | string[] | undefined, expected: string | undefined): boolean {
  if (expected === undefined) return value === undefined;
  return typeof value === "string" && value === expected;
}

// Burada çok değerli query parametrelerinde ilk değeri ayrıştırma katmanına taşıyorum.
function firstValue(value: string | string[] | undefined): string | undefined {
  return Array.isArray(value) ? value[0] : value;
}

// Burada parseInt'in kabul edeceği kısmi metinleri reddedip yalnız güvenli pozitif integer değerleri geçiriyorum.
function positiveInteger(value: string | undefined): number | null {
  if (!value || !/^[1-9]\d*$/.test(value)) return null;
  const parsed = Number(value);
  return Number.isSafeInteger(parsed) ? parsed : null;
}

// Burada API'nin PageSize üst sınırı olan 100'ü istemci URL'sinde de zorunlu tutuyorum.
function boundedPageSize(value: string | undefined): number | null {
  const parsed = positiveInteger(value);
  return parsed !== null && parsed <= COLLECTIONS_MAX_PAGE_SIZE ? parsed : null;
}
