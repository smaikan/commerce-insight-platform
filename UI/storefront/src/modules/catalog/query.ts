import type {
  CatalogSearchParams,
  CatalogSort,
  CatalogView,
  PublishedProductQuery,
} from "@/modules/catalog/types";

export const CATALOG_PAGE_SIZE = 24;

export const CATALOG_SORT_LABELS: Record<CatalogSort, string> = {
  newest: "En yeni",
  popular: "Çok Satanlar",
  "display-order": "Önerilen sıra",
  title: "Ada göre",
};

const SORT_QUERY: Record<CatalogSort, Pick<PublishedProductQuery, "SortBy" | "Descending">> = {
  newest: { SortBy: 0, Descending: true },
  popular: { SortBy: 1, Descending: true },
  "display-order": { SortBy: 2, Descending: false },
  title: { SortBy: 3, Descending: false },
};

const UUID_PATTERN = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

export type CatalogFilterKey = "brandId" | "collectionId" | "typeId";

export type CatalogUrlOptions = {
  basePath?: string;
  omitFilter?: CatalogFilterKey;
};

// Burada paylaşılabilir katalog görünümünü güvenli sayfa ve belgeli sıralama değerlerine indiriyorum.
export function parseCatalogView(searchParams: CatalogSearchParams): CatalogView {
  const rawPage = firstValue(searchParams.page);
  const page = Number.parseInt(rawPage || "1", 10);
  const rawSort = firstValue(searchParams.sort);
  const sort = isCatalogSort(rawSort) ? rawSort : "newest";
  const search = optionalSearch(searchParams.q);
  const hasExplicitSort = isCatalogSort(rawSort) && (Boolean(search) || rawSort !== "newest");

  const brandId = optionalUuid(searchParams.brand);
  const collectionId = optionalUuid(searchParams.collection);
  const typeId = optionalUuid(searchParams.type);

  return {
    page: Number.isSafeInteger(page) && page > 0 ? page : 1,
    sort,
    ...(hasExplicitSort ? { hasExplicitSort: true } : {}),
    ...(search ? { search } : {}),
    ...(brandId ? { brandId } : {}),
    ...(collectionId ? { collectionId } : {}),
    ...(typeId ? { typeId } : {}),
  };
}

// Burada UI görünümünü OpenAPI'nin sayfalama ve numeric enum sorgusuna dönüştürüyorum.
export function toPublishedProductQuery(view: CatalogView): PublishedProductQuery {
  // Burada aramada açık kullanıcı sıralaması yoksa SortBy göndermeyip backend relevance sırasını koruyorum.
  const shouldSendSort = !view.search || view.hasExplicitSort || view.sort !== "newest";
  return {
    PageNumber: view.page,
    PageSize: CATALOG_PAGE_SIZE,
    ...(shouldSendSort ? SORT_QUERY[view.sort] : {}),
    ...(view.search ? { Search: view.search } : {}),
    ...(view.brandId ? { BrandId: view.brandId } : {}),
    ...(view.collectionId ? { CollectionId: view.collectionId } : {}),
    ...(view.typeId ? { TypeId: view.typeId } : {}),
  };
}

// Burada sayfalama ve sıralama linklerinin yalnız anlamlı parametreleri taşımasını sağlıyorum.
export function catalogHref(view: CatalogView, options: CatalogUrlOptions = {}): string {
  const query = new URLSearchParams();
  if (view.search) query.set("q", view.search);
  if (view.page > 1) query.set("page", String(view.page));
  if (view.hasExplicitSort || view.sort !== "newest") query.set("sort", view.sort);
  if (view.brandId && options.omitFilter !== "brandId") query.set("brand", view.brandId);
  if (view.collectionId && options.omitFilter !== "collectionId") query.set("collection", view.collectionId);
  if (view.typeId && options.omitFilter !== "typeId") query.set("type", view.typeId);
  const suffix = query.toString();
  const basePath = options.basePath || "/products";
  return suffix ? `${basePath}?${suffix}` : basePath;
}

// Burada yalnız sıralamayı canonical URL'den çıkarıp filtrelenmiş ürün kümesini değiştirmeden koruyorum.
export function catalogCanonicalHref(view: CatalogView, options: CatalogUrlOptions = {}): string {
  return catalogHref({ ...view, sort: "newest", hasExplicitSort: false }, options);
}

export function hasCatalogFilters(view: CatalogView): boolean {
  return Boolean(view.brandId || view.collectionId || view.typeId);
}

// Burada bozuk, yinelenen, gereksiz veya tanınmayan katalog parametrelerini tek temiz URL'ye yönlendirmek için saptıyorum.
export function catalogSearchParamsNeedRedirect(
  searchParams: CatalogSearchParams,
  view: CatalogView,
  options: CatalogUrlOptions = {},
): boolean {
  const actualQuery = searchParamsToString(searchParams);
  const expectedQuery = new URLSearchParams(catalogHref(view, options).split("?", 2)[1] || "");
  expectedQuery.sort();
  return actualQuery !== expectedQuery.toString();
}
// Burada tek bir sınıflandırma filtresini kaldırırken sıralamayı ve diğer filtreleri koruyup sonucu ilk sayfaya döndürüyorum.
export function catalogHrefWithoutFilter(
  view: CatalogView,
  filter: CatalogFilterKey,
  options: CatalogUrlOptions = {},
): string {
  const nextView = { ...view, page: 1 };
  delete nextView[filter];
  return catalogHref(nextView, options);
}

// Burada gelen sorgu nesnesini anahtar sırasından bağımsız, yinelenen değerleri koruyan karşılaştırılabilir bir metne çeviriyorum.
function searchParamsToString(searchParams: CatalogSearchParams): string {
  const query = new URLSearchParams();

  for (const [key, value] of Object.entries(searchParams)) {
    if (Array.isArray(value)) {
      value.forEach((item) => query.append(key, item));
    } else if (value !== undefined) {
      query.append(key, value);
    }
  }

  query.sort();
  return query.toString();
}

// Burada çok değerli search param içinden yalnız ilk metin değerini okuyorum.
function firstValue(value: string | string[] | undefined): string | undefined {
  return Array.isArray(value) ? value[0] : value;
}

// Burada API'ye yalnız biçimi geçerli tekil GUID filtrelerinin ulaşmasına izin veriyorum.
function optionalUuid(value: string | string[] | undefined): string | undefined {
  const candidate = firstValue(value)?.trim();
  return candidate && UUID_PATTERN.test(candidate) ? candidate : undefined;
}

// Burada katalog URL'sinden yalnız backend'in kabul ettiği normalize 2-100 karakterli arama metnini alıyorum.
function optionalSearch(value: string | string[] | undefined): string | undefined {
  const candidate = firstValue(value)?.trim().replace(/\s+/g, " ");
  return candidate && candidate.length >= 2 && candidate.length <= 100 ? candidate : undefined;
}

// Burada kullanıcı URL'sindeki sıralama anahtarını desteklenen seçeneklerle sınırlandırıyorum.
function isCatalogSort(value: string | undefined): value is CatalogSort {
  return Boolean(value && Object.hasOwn(CATALOG_SORT_LABELS, value));
}
