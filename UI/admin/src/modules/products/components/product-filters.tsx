import Link from "next/link";
import {
  buildProductStatusTabHref,
  buildRemoveProductFilterHref,
  hasProductFilters,
  productSortOptions,
  productStatusOptions,
} from "@/modules/products/query";
import type { Brand, Collection, ProductListQuery, ProductType, Tag } from "@/modules/products/types";

const selectWrapperClass = "relative block min-w-0";
const selectControlClass =
  "min-h-9 w-full appearance-none rounded-lg border border-border-strong bg-surface-strong py-1.5 pl-3 pr-8 text-sm text-foreground outline-none transition-colors hover:border-border-strong/80 focus:border-primary focus:ring-2 focus:ring-primary/20";

// Burada Shopify benzeri sekmeli görünüm ve kompakt filtre araç çubuğu ile ürün yönetimini sunuyorum.
export function ProductFilters({
  query,
  productTypes,
  brands,
  collections,
  tags,
}: {
  query: ProductListQuery;
  productTypes: ProductType[];
  brands: Brand[];
  collections: Collection[];
  tags: Tag[];
}) {
  const selectedSort =
    productSortOptions.find(
      (option) => option.sortBy === query.sortBy && option.descending === query.descending,
    )?.value || productSortOptions[0].value;

  const activeTypeName = productTypes.find((t) => t.id === query.typeId)?.name;
  const activeBrandName = brands.find((b) => b.id === query.brandId)?.name;
  const activeCollectionName = collections.find((c) => c.id === query.collectionId)?.name;
  const activeTagName = tags.find((t) => t.id === query.tagId)?.name;
  const activeStatusLabel = productStatusOptions.find((s) => s.value === query.status)?.label;

  const isAnyFilterActive = hasProductFilters(query);

  const currentTab =
    query.isFeatured === true
      ? "featured"
      : query.status === 1
        ? "active"
        : query.status === 0
          ? "draft"
          : query.status === 2
            ? "passive"
            : query.status === 3
              ? "archived"
              : "all";

  const statusTabs = [
    { id: "all", label: "Tümü", href: buildProductStatusTabHref(query, "all") },
    { id: "active", label: "Aktif", href: buildProductStatusTabHref(query, "active") },
    { id: "draft", label: "Taslak", href: buildProductStatusTabHref(query, "draft") },
    { id: "passive", label: "Pasif", href: buildProductStatusTabHref(query, "passive") },
    { id: "archived", label: "Arşivlenmiş", href: buildProductStatusTabHref(query, "archived") },
    { id: "featured", label: "Öne Çıkanlar", href: buildProductStatusTabHref(query, "featured") },
  ] as const;

  return (
    <div className="border-b border-border bg-surface">
      {/* Görünüm / Durum Hızlı Filtre Sekmeleri */}
      <nav aria-label="Ürün görünüm filtreleri" className="flex items-center gap-1 overflow-x-auto border-b border-border px-4 pt-2 sm:px-5">
        {statusTabs.map((tab) => {
          const isActive = currentTab === tab.id;
          return (
            <Link
              key={tab.id}
              href={tab.href}
              className={`inline-flex shrink-0 items-center border-b-2 px-3 py-2 text-xs font-semibold transition-colors ${
                isActive
                  ? "border-primary text-primary"
                  : "border-transparent text-muted hover:border-border-strong hover:text-foreground"
              }`}
            >
              {tab.label}
            </Link>
          );
        })}
      </nav>

      {/* Ana Filtreleme Formu */}
      <form action="/products" method="get" className="p-3.5 sm:p-4">
        {/* Durum veya Öne Çıkarılma sekmesi seçiliyse form submit'inde korunsun */}
        {query.status !== undefined ? <input type="hidden" name="status" value={query.status} /> : null}
        {query.isFeatured !== undefined ? <input type="hidden" name="isFeatured" value={String(query.isFeatured)} /> : null}

        <div className="grid gap-2.5 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 xl:grid-cols-6">
          {/* Arama Alanı */}
          <div className="sm:col-span-2 md:col-span-3 lg:col-span-4 xl:col-span-2">
            <label htmlFor="product-search" className="sr-only">Ürün Ara</label>
            <div className="relative">
              <svg
                aria-hidden="true"
                viewBox="0 0 24 24"
                className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 fill-none stroke-muted stroke-2"
              >
                <circle cx="11" cy="11" r="7" />
                <path d="m16 16 4 4" strokeLinecap="round" />
              </svg>
              <input
                id="product-search"
                name="search"
                type="search"
                maxLength={250}
                defaultValue={query.search}
                placeholder="Başlık, URL veya ana SKU ara..."
                className="min-h-9 w-full rounded-lg border border-border-strong bg-surface-strong pl-9 pr-3 text-sm text-foreground outline-none transition-colors placeholder:text-muted focus:border-primary focus:ring-2 focus:ring-primary/20"
                autoComplete="off"
              />
            </div>
          </div>

          {/* Ürün Tipi */}
          <div className={selectWrapperClass}>
            <label htmlFor="product-type" className="sr-only">Ürün Tipi</label>
            <select id="product-type" name="typeId" defaultValue={query.typeId || ""} className={selectControlClass}>
              <option value="">Tüm Tipler</option>
              {productTypes.map((type) => (
                <option key={type.id} value={type.id}>
                  {type.name}
                </option>
              ))}
            </select>
            <SelectChevron />
          </div>

          {/* Marka */}
          <div className={selectWrapperClass}>
            <label htmlFor="product-brand" className="sr-only">Marka</label>
            <select id="product-brand" name="brandId" defaultValue={query.brandId || ""} className={selectControlClass}>
              <option value="">Tüm Markalar</option>
              {brands.map((brand) => (
                <option key={brand.id} value={brand.id}>
                  {brand.name}
                </option>
              ))}
            </select>
            <SelectChevron />
          </div>

          {/* Koleksiyon */}
          <div className={selectWrapperClass}>
            <label htmlFor="product-collection" className="sr-only">Koleksiyon</label>
            <select id="product-collection" name="collectionId" defaultValue={query.collectionId || ""} className={selectControlClass}>
              <option value="">Tüm Koleksiyonlar</option>
              {collections.map((col) => (
                <option key={col.id} value={col.id}>
                  {col.name}
                </option>
              ))}
            </select>
            <SelectChevron />
          </div>

          {/* Etiket */}
          <div className={selectWrapperClass}>
            <label htmlFor="product-tag" className="sr-only">Etiket</label>
            <select id="product-tag" name="tagId" defaultValue={query.tagId || ""} className={selectControlClass}>
              <option value="">Tüm Etiketler</option>
              {tags.map((tag) => (
                <option key={tag.id} value={tag.id}>
                  #{tag.name}
                </option>
              ))}
            </select>
            <SelectChevron />
          </div>

          {/* Sıralama */}
          <div className={selectWrapperClass}>
            <label htmlFor="product-sort" className="sr-only">Sıralama</label>
            <select id="product-sort" name="sort" defaultValue={selectedSort} className={selectControlClass}>
              {productSortOptions.map((option) => (
                <option key={option.value} value={option.value}>
                  {option.label}
                </option>
              ))}
            </select>
            <SelectChevron />
          </div>

          {/* Sayfa Boyutu */}
          <div className={selectWrapperClass}>
            <label htmlFor="product-page-size" className="sr-only">Sayfa Boyutu</label>
            <select id="product-page-size" name="pageSize" defaultValue={query.pageSize} className={selectControlClass}>
              {[10, 20, 50, 100].map((size) => (
                <option key={size} value={size}>
                  {size} ürün / sayfa
                </option>
              ))}
            </select>
            <SelectChevron />
          </div>

          {/* Aksiyon Butonları */}
          <div className="flex items-center gap-2 sm:col-span-2 md:col-span-1 xl:col-span-2">
            <button
              type="submit"
              className="inline-flex min-h-9 flex-1 cursor-pointer items-center justify-center gap-1.5 rounded-lg bg-primary px-3.5 text-xs font-semibold text-white shadow-xs transition-colors hover:bg-primary-hover focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary focus-visible:ring-offset-2"
            >
              <svg aria-hidden="true" viewBox="0 0 20 20" fill="currentColor" className="size-3.5">
                <path
                  fillRule="evenodd"
                  d="M2.628 1.601C5.028 1.206 7.49 1 10 1s4.973.206 7.372.601a.75.75 0 0 1 .628.74v2.288a2.25 2.25 0 0 1-.659 1.59l-4.682 4.683a2.25 2.25 0 0 0-.659 1.59v3.037c0 .684-.31 1.33-.844 1.757l-1.937 1.55A.75.75 0 0 1 8 18.25v-5.757a2.25 2.25 0 0 0-.659-1.591L2.659 6.22A2.25 2.25 0 0 1 2 4.629V2.34a.75.75 0 0 1 .628-.74Z"
                  clipRule="evenodd"
                />
              </svg>
              <span>Filtrele</span>
            </button>

            {isAnyFilterActive ? (
              <Link
                href="/products"
                className="inline-flex min-h-9 cursor-pointer items-center justify-center rounded-lg border border-border-strong bg-surface-strong px-3 text-xs font-medium text-foreground transition-colors hover:bg-surface-subtle focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary focus-visible:ring-offset-2"
              >
                Temizle
              </Link>
            ) : null}
          </div>
        </div>

        {/* Aktif Filtre Çipleri */}
        {isAnyFilterActive ? (
          <div className="mt-3 flex flex-wrap items-center gap-1.5 border-t border-border/60 pt-2.5">
            <span className="text-[11px] font-semibold uppercase tracking-wider text-muted">Aktif:</span>

            {query.search ? (
              <FilterChip
                label={`Arama: "${query.search}"`}
                removeHref={buildRemoveProductFilterHref(query, "search")}
                ariaLabel="Arama filtresini kaldır"
              />
            ) : null}

            {query.typeId && activeTypeName ? (
              <FilterChip
                label={`Tip: ${activeTypeName}`}
                removeHref={buildRemoveProductFilterHref(query, "typeId")}
                ariaLabel="Ürün tipi filtresini kaldır"
              />
            ) : null}

            {query.brandId && activeBrandName ? (
              <FilterChip
                label={`Marka: ${activeBrandName}`}
                removeHref={buildRemoveProductFilterHref(query, "brandId")}
                ariaLabel="Marka filtresini kaldır"
              />
            ) : null}

            {query.collectionId && activeCollectionName ? (
              <FilterChip
                label={`Koleksiyon: ${activeCollectionName}`}
                removeHref={buildRemoveProductFilterHref(query, "collectionId")}
                ariaLabel="Koleksiyon filtresini kaldır"
              />
            ) : null}

            {query.tagId && activeTagName ? (
              <FilterChip
                label={`Etiket: #${activeTagName}`}
                removeHref={buildRemoveProductFilterHref(query, "tagId")}
                ariaLabel="Etiket filtresini kaldır"
              />
            ) : null}

            {query.status !== undefined && activeStatusLabel ? (
              <FilterChip
                label={`Durum: ${activeStatusLabel}`}
                removeHref={buildRemoveProductFilterHref(query, "status")}
                ariaLabel="Durum filtresini kaldır"
              />
            ) : null}

            {query.isFeatured !== undefined ? (
              <FilterChip
                label={query.isFeatured ? "Öne Çıkarılan" : "Öne Çıkarılmayan"}
                removeHref={buildRemoveProductFilterHref(query, "isFeatured")}
                ariaLabel="Öne çıkarma filtresini kaldır"
              />
            ) : null}

            <Link
              href="/products"
              className="ml-1 text-xs font-semibold text-primary underline-offset-4 hover:underline focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-primary"
            >
              Tümünü Temizle
            </Link>
          </div>
        ) : null}
      </form>
    </div>
  );
}

// Burada select elemanları için tutarlı ve zarif aşağı ok ikonunu sunuyorum.
function SelectChevron() {
  return (
    <svg
      aria-hidden="true"
      viewBox="0 0 20 20"
      fill="currentColor"
      className="pointer-events-none absolute right-2.5 top-1/2 size-4 -translate-y-1/2 text-muted"
    >
      <path
        fillRule="evenodd"
        d="M5.22 8.22a.75.75 0 0 1 1.06 0L10 11.94l3.72-3.72a.75.75 0 1 1 1.06 1.06l-4.25 4.25a.75.75 0 0 1-1.06 0L5.22 9.28a.75.75 0 0 1 0-1.06Z"
        clipRule="evenodd"
      />
    </svg>
  );
}

// Burada aktif filtrenin tek tıkla kaldırılabilmesini sağlayan kompakt çip bileşenini sunuyorum.
function FilterChip({
  label,
  removeHref,
  ariaLabel,
}: {
  label: string;
  removeHref: string;
  ariaLabel: string;
}) {
  return (
    <span className="inline-flex items-center gap-1 rounded-md border border-border-strong/70 bg-surface-subtle py-0.5 pl-2 pr-1 text-xs font-medium text-foreground">
      <span>{label}</span>
      <Link
        href={removeHref}
        aria-label={ariaLabel}
        className="inline-flex size-3.5 items-center justify-center rounded text-muted transition-colors hover:bg-surface-strong hover:text-foreground focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-primary"
      >
        <svg aria-hidden="true" viewBox="0 0 14 14" className="size-2.5 stroke-current stroke-2 fill-none">
          <path d="m3 3 8 8M11 3 3 11" strokeLinecap="round" />
        </svg>
      </Link>
    </span>
  );
}
