import type { ProductListQuery, ProductSortBy, ProductStatus } from "@/modules/products/types";

export const productSortOptions = [
  { value: "created-desc", label: "Oluşturma: yeniden eskiye", sortBy: 2, descending: true },
  { value: "popularity-desc", label: "Popülerlik: yüksekten düşüğe", sortBy: 3, descending: true },
  { value: "display-order-asc", label: "Görüntüleme sırası: artan", sortBy: 0, descending: false },
  { value: "display-order-desc", label: "Görüntüleme sırası: azalan", sortBy: 0, descending: true },
  { value: "title-asc", label: "Başlık: A-Z", sortBy: 1, descending: false },
  { value: "title-desc", label: "Başlık: Z-A", sortBy: 1, descending: true },
  { value: "created-asc", label: "Oluşturma: eskiden yeniye", sortBy: 2, descending: false },
] as const satisfies ReadonlyArray<{
  value: string;
  label: string;
  sortBy: ProductSortBy;
  descending: boolean;
}>;

export const productStatusOptions = [
  { value: 0, label: "Taslak" },
  { value: 1, label: "Aktif" },
  { value: 2, label: "Pasif" },
  { value: 3, label: "Arşivlenmiş" },
] as const satisfies ReadonlyArray<{ value: ProductStatus; label: string }>;

type SearchParams = Record<string, string | string[] | undefined>;

// Burada ürün listesi URL parametrelerini backend sınırları içinde güvenli varsayılanlara dönüştürüyorum.
export function parseProductListQuery(searchParams: SearchParams): ProductListQuery {
  const sort = productSortOptions.find((option) => option.value === first(searchParams.sort)) ?? productSortOptions[0];

  return {
    pageNumber: boundedInteger(first(searchParams.page), 1, 1, Number.MAX_SAFE_INTEGER),
    pageSize: boundedInteger(first(searchParams.pageSize), 20, 1, 100),
    search: clean(first(searchParams.search), 250),
    typeId: optionalUuid(first(searchParams.typeId)),
    brandId: optionalUuid(first(searchParams.brandId)),
    status: enumNumber(first(searchParams.status), [0, 1, 2, 3]),
    isFeatured: optionalBoolean(first(searchParams.isFeatured)),
    sortBy: sort.sortBy,
    descending: sort.descending,
  };
}

// Burada geçerli sorgu durumunu sayfalama bağlantılarında kayıp olmadan yeniden oluşturuyorum.
export function buildProductListHref(query: ProductListQuery, pageNumber: number): string {
  const params = new URLSearchParams();
  params.set("page", String(pageNumber));
  params.set("pageSize", String(query.pageSize));
  if (query.search) params.set("search", query.search);
  if (query.typeId) params.set("typeId", query.typeId);
  if (query.brandId) params.set("brandId", query.brandId);
  if (query.status !== undefined) params.set("status", String(query.status));
  if (query.isFeatured !== undefined) params.set("isFeatured", String(query.isFeatured));
  const sort = productSortOptions.find(
    (option) => option.sortBy === query.sortBy && option.descending === query.descending,
  );
  if (sort) params.set("sort", sort.value);
  return `/products?${params.toString()}`;
}

// Burada liste üzerinde etkin bir filtre bulunup bulunmadığını boş durum metni için belirliyorum.
export function hasProductFilters(query: ProductListQuery): boolean {
  return Boolean(
    query.search ||
      query.typeId ||
      query.brandId ||
      query.status !== undefined ||
      query.isFeatured !== undefined,
  );
}

// Burada çoklu search param değerlerinde yalnız ilk güvenli değeri kullanıyorum.
function first(value: string | string[] | undefined): string | undefined {
  return Array.isArray(value) ? value[0] : value;
}

// Burada metin parametrelerini boşluk ve maksimum uzunluk sınırında temizliyorum.
function clean(value: string | undefined, maxLength: number): string | undefined {
  const normalized = value?.trim().slice(0, maxLength);
  return normalized || undefined;
}

// Burada ürün tipi ve marka filtrelerine yalnız geçerli UUID değerlerinin ulaşmasını sağlıyorum.
function optionalUuid(value: string | undefined): string | undefined {
  const normalized = clean(value, 36);
  return normalized && /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i.test(normalized)
    ? normalized
    : undefined;
}

// Burada sayısal URL parametrelerini kabul edilen aralıkta tutuyorum.
function boundedInteger(value: string | undefined, fallback: number, min: number, max: number): number {
  const parsed = Number.parseInt(value || "", 10);
  return Number.isInteger(parsed) && parsed >= min && parsed <= max ? parsed : fallback;
}

// Burada yalnız belgelenen enum sayılarının sorguya geçmesine izin veriyorum.
function enumNumber<T extends number>(value: string | undefined, allowed: readonly T[]): T | undefined {
  const parsed = Number(value);
  return allowed.includes(parsed as T) ? (parsed as T) : undefined;
}

// Burada üç durumlu boolean filtreyi boş, doğru ve yanlış olarak ayırıyorum.
function optionalBoolean(value: string | undefined): boolean | undefined {
  if (value === "true") return true;
  if (value === "false") return false;
  return undefined;
}
