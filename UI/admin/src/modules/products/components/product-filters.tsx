import Link from "next/link";
import { hasProductFilters, productSortOptions, productStatusOptions } from "@/modules/products/query";
import type { Brand, ProductListQuery, ProductType } from "@/modules/products/types";

const controlClass =
  "min-h-10 rounded-lg border border-border-strong bg-surface-strong px-3 text-sm text-foreground outline-none focus:border-primary sm:min-h-9";

// Burada yalnız backend'in belgelediği ürün filtrelerini URL tabanlı GET formunda topluyorum.
export function ProductFilters({
  query,
  productTypes,
  brands,
}: {
  query: ProductListQuery;
  productTypes: ProductType[];
  brands: Brand[];
}) {
  const selectedSort =
    productSortOptions.find(
      (option) => option.sortBy === query.sortBy && option.descending === query.descending,
    )?.value || productSortOptions[0].value;

  return (
    <form action="/products" method="get" className="border-b border-border bg-surface-subtle/60 p-4 sm:p-5">
      <div className="grid gap-3 lg:grid-cols-[minmax(16rem,1.6fr)_repeat(3,minmax(9rem,0.7fr))]">
        <label className="relative block">
          <span className="sr-only">Ürün ara</span>
          <svg aria-hidden="true" viewBox="0 0 24 24" className="absolute left-3 top-1/2 size-4 -translate-y-1/2 fill-none stroke-muted stroke-2">
            <circle cx="11" cy="11" r="7" />
            <path d="m16 16 4 4" strokeLinecap="round" />
          </svg>
          <input
            name="search"
            type="search"
            maxLength={250}
            defaultValue={query.search}
            placeholder="Başlık, URL veya ana SKU ara"
            className={`${controlClass} w-full pl-10`}
          />
        </label>

        <label>
          <span className="sr-only">Ürün tipi</span>
          <select name="typeId" defaultValue={query.typeId || ""} className={`${controlClass} w-full`}>
            <option value="">Tüm ürün tipleri</option>
            {productTypes.map((type) => (
              <option key={type.id} value={type.id}>{type.name}</option>
            ))}
          </select>
        </label>

        <label>
          <span className="sr-only">Marka</span>
          <select name="brandId" defaultValue={query.brandId || ""} className={`${controlClass} w-full`}>
            <option value="">Tüm markalar</option>
            {brands.map((brand) => (
              <option key={brand.id} value={brand.id}>{brand.name}</option>
            ))}
          </select>
        </label>

        <label>
          <span className="sr-only">Ürün durumu</span>
          <select name="status" defaultValue={query.status ?? ""} className={`${controlClass} w-full`}>
            <option value="">Tüm durumlar</option>
            {productStatusOptions.map((status) => (
              <option key={status.value} value={status.value}>{status.label}</option>
            ))}
          </select>
        </label>
      </div>

      <div className="mt-3 grid gap-3 sm:grid-cols-2 xl:grid-cols-[minmax(11rem,0.8fr)_minmax(17rem,1.25fr)_minmax(12rem,0.8fr)_8rem_auto]">
        <label>
          <span className="sr-only">Öne çıkarılma</span>
          <select name="isFeatured" defaultValue={query.isFeatured === undefined ? "" : String(query.isFeatured)} className={`${controlClass} w-full`}>
            <option value="">Tümü</option>
            <option value="true">Öne çıkarılanlar</option>
            <option value="false">Öne çıkarılmayanlar</option>
          </select>
        </label>

        <label>
          <span className="sr-only">Sıralama</span>
          <select name="sort" defaultValue={selectedSort} className={`${controlClass} w-full`}>
            {productSortOptions.map((option) => (
              <option key={option.value} value={option.value}>{option.label}</option>
            ))}
          </select>
        </label>

        <label>
          <span className="sr-only">Sayfa başına ürün</span>
          <select name="pageSize" defaultValue={query.pageSize} className={`${controlClass} w-full`}>
            {[10, 20, 50, 100].map((size) => (
              <option key={size} value={size}>{size} ürün / sayfa</option>
            ))}
          </select>
        </label>

        <button type="submit" className="min-h-10 rounded-lg bg-primary px-4 text-sm font-semibold text-white hover:bg-primary-hover sm:min-h-9">
          Uygula
        </button>
        {hasProductFilters(query) ? (
          <Link href="/products" className="inline-flex min-h-10 items-center justify-center rounded-lg border border-border-strong bg-surface-strong px-4 text-sm font-medium text-foreground hover:bg-surface-subtle sm:min-h-9">
            Temizle
          </Link>
        ) : null}
      </div>
    </form>
  );
}
