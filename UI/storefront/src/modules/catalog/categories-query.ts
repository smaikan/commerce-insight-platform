export const CATEGORIES_DEFAULT_PAGE_SIZE = 20;
export const CATEGORIES_MAX_PAGE_SIZE = 100;

export type CategoriesSearchParams = Record<string, string | string[] | undefined>;

export type CategoriesView = {
  page: number;
  pageSize: number;
};

// Burada URL sayfalamasını API'nin pozitif PageNumber ve 1..100 PageSize sınırlarına indiriyorum.
export function parseCategoriesView(searchParams: CategoriesSearchParams): CategoriesView {
  return {
    page: positiveInteger(firstValue(searchParams.page)) ?? 1,
    pageSize: boundedPageSize(firstValue(searchParams.pageSize)) ?? CATEGORIES_DEFAULT_PAGE_SIZE,
  };
}

// Burada bozuk, çok değerli veya gereksiz varsayılan parametreleri temiz kategori URL'sine yönlendirmek için saptıyorum.
export function categoriesSearchParamsNeedRedirect(
  searchParams: CategoriesSearchParams,
  view: CategoriesView,
): boolean {
  return !matchesSingleValue(searchParams.page, view.page > 1 ? String(view.page) : undefined)
    || !matchesSingleValue(
      searchParams.pageSize,
      view.pageSize !== CATEGORIES_DEFAULT_PAGE_SIZE ? String(view.pageSize) : undefined,
    );
}

// Burada sayfalama bağlantılarında varsayılanları gizleyip seçilmiş geçerli sayfa boyutunu koruyorum.
export function categoriesHref(view: CategoriesView): string {
  const query = new URLSearchParams();
  if (view.page > 1) query.set("page", String(view.page));
  if (view.pageSize !== CATEGORIES_DEFAULT_PAGE_SIZE) query.set("pageSize", String(view.pageSize));
  const suffix = query.toString();
  return suffix ? `/categories?${suffix}` : "/categories";
}

// Burada yalnızca tek URL değerinin beklenen temiz değerle birebir eşleşmesini kabul ediyorum.
function matchesSingleValue(value: string | string[] | undefined, expected: string | undefined): boolean {
  if (expected === undefined) return value === undefined;
  return typeof value === "string" && value === expected;
}

// Burada çok değerli sorgu parametrelerinin ilk değerini ayrıştırma katmanına taşıyorum.
function firstValue(value: string | string[] | undefined): string | undefined {
  return Array.isArray(value) ? value[0] : value;
}

// Burada kısmi sayısal metinleri reddedip yalnızca güvenli pozitif tam sayıları geçiriyorum.
function positiveInteger(value: string | undefined): number | null {
  if (!value || !/^[1-9]\d*$/.test(value)) return null;
  const parsed = Number(value);
  return Number.isSafeInteger(parsed) ? parsed : null;
}

// Burada API'nin PageSize üst sınırı olan 100'ü istemci URL'sinde de uyguluyorum.
function boundedPageSize(value: string | undefined): number | null {
  const parsed = positiveInteger(value);
  return parsed !== null && parsed <= CATEGORIES_MAX_PAGE_SIZE ? parsed : null;
}
