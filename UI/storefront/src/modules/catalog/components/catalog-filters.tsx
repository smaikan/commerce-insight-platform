import Link from "next/link";
import type { ReactNode } from "react";

import {
  catalogHref,
  catalogHrefWithoutFilter,
  type CatalogFilterKey,
  type CatalogUrlOptions,
} from "../query";
import type { CatalogFacets, CatalogView } from "../types";

// Burada filtreleri lüks, modern ve JavaScript gerektirmeyen bir GET formuyla paylaşılabilir katalog URL'lerine bağlıyorum.
export function CatalogFilters({
  facets,
  view,
  urlOptions,
}: {
  facets: CatalogFacets;
  view: CatalogView;
  urlOptions?: CatalogUrlOptions;
}) {
  const activeFilters: Array<{ key: CatalogFilterKey; label: string; value: string }> = [];

  if (view.brandId && urlOptions?.omitFilter !== "brandId") {
    activeFilters.push({
      key: "brandId",
      label: "Marka",
      value: facets.brands.find((brand) => brand.id === view.brandId)?.name || "Kullanılamıyor",
    });
  }
  if (view.collectionId && urlOptions?.omitFilter !== "collectionId") {
    activeFilters.push({
      key: "collectionId",
      label: "Koleksiyon",
      value: facets.collections.find((collection) => collection.id === view.collectionId)?.name || "Kullanılamıyor",
    });
  }
  if (view.typeId && urlOptions?.omitFilter !== "typeId") {
    activeFilters.push({
      key: "typeId",
      label: "Ürün türü",
      value: facets.productTypes.find((productType) => productType.id === view.typeId)?.name || "Kullanılamıyor",
    });
  }
  const hasFilters = activeFilters.length > 0;

  return (
    <section aria-labelledby="catalog-filters-heading" className="my-4">
      <h2 id="catalog-filters-heading" className="sr-only">Ürünleri filtrele</h2>
      <details className="group rounded-2xl border border-line/80 bg-surface/90 shadow-xs transition-all">
        <summary className="focus-ring flex min-h-14 cursor-pointer list-none items-center justify-between gap-4 px-5 text-sm font-bold text-ink [&::-webkit-details-marker]:hidden">
          <span className="flex min-w-0 items-center gap-3">
            <svg aria-hidden="true" viewBox="0 0 24 24" className="size-5 shrink-0 text-brand-700" fill="none" stroke="currentColor" strokeWidth="1.7" strokeLinecap="round">
              <path d="M4 6h16M7 12h10M10 18h4" />
            </svg>
            <span className="tracking-tight">Detaylı Filtreler</span>
            {activeFilters.length > 0 ? (
              <span className="rounded-full bg-brand-950 px-2.5 py-0.5 text-[0.6875rem] font-bold text-white shadow-xs">
                {activeFilters.length} seçili
              </span>
            ) : null}
          </span>
          <div className="flex items-center gap-2">
            <span className="text-xs text-ink-muted group-open:hidden">Filtreleri Göster</span>
            <span className="text-xs text-ink-muted hidden group-open:inline">Kapat</span>
            <svg aria-hidden="true" viewBox="0 0 24 24" className="size-4 shrink-0 text-ink-muted transition-transform duration-300 group-open:rotate-180" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round">
              <path d="m6 9 6 6 6-6" />
            </svg>
          </div>
        </summary>

        <div className="border-t border-line/70 p-5 sm:p-6 bg-surface-subtle/30 rounded-b-2xl">
          <div className="mb-5 flex items-center justify-between gap-4">
            <p className="text-xs text-ink-muted">
              Marka, koleksiyon ve ürün türü kriterlerini birlikte filtreleyebilirsiniz.
            </p>

            {hasFilters ? (
              <Link
                className="focus-ring text-xs font-bold text-brand-700 hover:text-brand-950 underline underline-offset-4 transition-colors"
                href={catalogHref({ ...view, page: 1, brandId: undefined, collectionId: undefined, typeId: undefined }, urlOptions)}
                prefetch={false}
              >
                Tüm filtreleri temizle
              </Link>
            ) : null}
          </div>

          <form
            action={urlOptions?.basePath || "/products"}
            method="get"
            className={`grid gap-4 sm:grid-cols-2 lg:items-end ${
              urlOptions?.omitFilter
                ? "lg:grid-cols-[repeat(2,minmax(0,1fr))_auto]"
                : "lg:grid-cols-[repeat(3,minmax(0,1fr))_auto]"
            }`}
          >
            {view.search ? <input type="hidden" name="q" value={view.search} /> : null}
            {view.hasExplicitSort || view.sort !== "newest" ? <input type="hidden" name="sort" value={view.sort} /> : null}

            {urlOptions?.omitFilter !== "brandId" ? (
              <FilterSelect label="Marka" name="brand" emptyLabel="Tüm markalar" defaultValue={view.brandId}>
                {facets.brands.map((brand) => <option key={brand.id} value={brand.id}>{brand.name} ({brand.productCount})</option>)}
              </FilterSelect>
            ) : null}

            {urlOptions?.omitFilter !== "collectionId" ? (
              <FilterSelect label="Koleksiyon" name="collection" emptyLabel="Tüm koleksiyonlar" defaultValue={view.collectionId}>
                {facets.collections.map((collection) => <option key={collection.id} value={collection.id}>{collection.name} ({collection.productCount})</option>)}
              </FilterSelect>
            ) : null}

            {urlOptions?.omitFilter !== "typeId" ? (
              <FilterSelect label="Ürün türü" name="type" emptyLabel="Tüm ürün türleri" defaultValue={view.typeId}>
                {facets.productTypes.map((productType) => <option key={productType.id} value={productType.id}>{productType.name} ({productType.productCount})</option>)}
              </FilterSelect>
            ) : null}

            <button
              type="submit"
              className="focus-ring min-h-11 cursor-pointer rounded-xl bg-brand-950 px-6 py-2.5 text-xs font-bold text-white shadow-xs transition-all hover:bg-brand-700 sm:col-span-2 lg:col-span-1"
            >
              Filtreleri Uygula
            </button>
          </form>
        </div>
      </details>

      {activeFilters.length > 0 ? (
        <ul className="flex flex-wrap gap-2 pt-3" aria-label="Seçili ürün filtreleri">
          {activeFilters.map((filter) => (
            <li key={filter.key}>
              <Link
                href={catalogHrefWithoutFilter(view, filter.key, urlOptions)}
                prefetch={false}
                aria-label={`${filter.label}: ${filter.value} filtresini kaldır`}
                className="focus-ring inline-flex min-h-9 cursor-pointer items-center gap-2 rounded-xl border border-line/80 bg-surface px-3 py-1 text-xs text-ink hover:border-brand-700 hover:shadow-xs transition-all group"
              >
                <span className="text-ink-muted">{filter.label}:</span>
                <span className="font-bold">{filter.value}</span>
                <span aria-hidden="true" className="flex size-4 items-center justify-center rounded-full bg-surface-subtle text-ink-muted group-hover:bg-red-50 group-hover:text-red-700">
                  &times;
                </span>
              </Link>
            </li>
          ))}
        </ul>
      ) : null}
    </section>
  );
}

function FilterSelect({
  label,
  name,
  emptyLabel,
  defaultValue,
  children,
}: {
  label: string;
  name: string;
  emptyLabel: string;
  defaultValue?: string;
  children: ReactNode;
}) {
  return (
    <label className="grid gap-1.5 text-xs font-bold text-ink">
      <span>{label}</span>
      <select
        name={name}
        defaultValue={defaultValue || ""}
        className="focus-ring min-h-11 w-full rounded-xl border border-line/80 bg-surface px-3.5 text-xs font-medium text-ink shadow-2xs hover:border-brand-700 transition-colors"
      >
        <option value="">{emptyLabel}</option>
        {children}
      </select>
    </label>
  );
}
