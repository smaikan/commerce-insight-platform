import Link from "next/link";
import type { ReactNode } from "react";

import {
  catalogHref,
  catalogHrefWithoutFilter,
  type CatalogFilterKey,
  type CatalogUrlOptions,
} from "../query";
import type { CatalogFacets, CatalogView } from "../types";

// Burada filtreleri JavaScript gerektirmeyen bir GET formuyla paylaşılabilir katalog URL'lerine bağlıyorum.
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

  // Burada kapalı filtre panelinde de seçili sınıflandırmaların adlarını gösterebilmek için API seçeneklerinden güvenli etiketler çözüyorum.
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

  return (
    <section aria-labelledby="catalog-filters-heading">
      <h2 id="catalog-filters-heading" className="sr-only">Ürünleri filtrele</h2>
      <details className="group border-y border-line bg-surface/55">
        <summary className="focus-ring flex min-h-14 cursor-pointer list-none items-center justify-between gap-4 px-1 text-sm font-bold text-ink [&::-webkit-details-marker]:hidden">
          <span className="flex min-w-0 items-center gap-3">
            <svg aria-hidden="true" viewBox="0 0 24 24" className="size-5 shrink-0 text-brand-700" fill="none" stroke="currentColor" strokeWidth="1.7" strokeLinecap="round">
              <path d="M4 6h16M7 12h10M10 18h4" />
            </svg>
            <span>Filtreler</span>
            {activeFilters.length > 0 ? (
              <span className="rounded-full bg-surface-subtle px-2 py-1 text-[0.6875rem] font-semibold text-brand-700">
                {activeFilters.length} seçili
              </span>
            ) : null}
          </span>
          <svg aria-hidden="true" viewBox="0 0 24 24" className="size-5 shrink-0 text-ink-muted transition-transform group-open:rotate-180" fill="none" stroke="currentColor" strokeWidth="1.7" strokeLinecap="round">
            <path d="m7 9 5 5 5-5" />
          </svg>
        </summary>

        <div className="border-t border-line py-5">
          <div className="mb-4 flex items-start justify-between gap-4">
            <p className="max-w-lg text-xs leading-5 text-ink-muted">Marka, koleksiyon ve ürün türünü birlikte seçebilirsiniz.</p>
            {activeFilters.length > 0 ? (
              <Link className="focus-ring shrink-0 text-sm font-semibold text-brand-700 hover:text-brand-950" href={catalogHref({ page: 1, sort: view.sort }, urlOptions)}>
                Temizle
              </Link>
            ) : null}
          </div>

          <form action={urlOptions?.basePath || "/products"} method="get" className="grid gap-3 sm:grid-cols-2 lg:grid-cols-[repeat(3,minmax(0,1fr))_auto] lg:items-end">
            {view.sort !== "newest" ? <input type="hidden" name="sort" value={view.sort} /> : null}

            {/* Burada yolun zaten temsil ettiği sınıflandırma filtresini ikinci kez seçilebilir olarak göstermiyorum. */}
            {urlOptions?.omitFilter !== "brandId" ? (
              <FilterSelect label="Marka" name="brand" emptyLabel="Tüm markalar" defaultValue={view.brandId}>
                {facets.brands.map((brand) => <option key={brand.id} value={brand.id}>{brand.name}</option>)}
              </FilterSelect>
            ) : null}

            {urlOptions?.omitFilter !== "collectionId" ? (
              <FilterSelect label="Koleksiyon" name="collection" emptyLabel="Tüm koleksiyonlar" defaultValue={view.collectionId}>
                {facets.collections.map((collection) => <option key={collection.id} value={collection.id}>{collection.name}</option>)}
              </FilterSelect>
            ) : null}

            {urlOptions?.omitFilter !== "typeId" ? (
              <FilterSelect label="Ürün türü" name="type" emptyLabel="Tüm ürün türleri" defaultValue={view.typeId}>
                {facets.productTypes.map((productType) => <option key={productType.id} value={productType.id}>{productType.name}</option>)}
              </FilterSelect>
            ) : null}

            <button type="submit" className="focus-ring min-h-11 rounded-lg bg-brand-700 px-5 py-2.5 text-sm font-bold text-white transition-colors hover:bg-brand-950 sm:col-span-2 lg:col-span-1">
              Uygula
            </button>
          </form>
        </div>
      </details>

      {activeFilters.length > 0 ? (
        <ul className="flex flex-wrap gap-2 border-b border-line py-3" aria-label="Seçili ürün filtreleri">
          {activeFilters.map((filter) => (
            <li key={filter.key}>
              <Link
                href={catalogHrefWithoutFilter(view, filter.key, urlOptions)}
                aria-label={`${filter.label}: ${filter.value} filtresini kaldır`}
                className="focus-ring inline-flex min-h-10 items-center gap-2 rounded-full border border-line bg-surface px-3 py-1.5 text-xs text-ink hover:border-brand-600"
              >
                <span className="text-ink-muted">{filter.label}</span>
                <span className="font-bold">{filter.value}</span>
                <svg aria-hidden="true" viewBox="0 0 20 20" className="size-3.5 shrink-0 text-brand-700" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round">
                  <path d="m6 6 8 8M14 6l-8 8" />
                </svg>
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
    <label className="grid gap-1.5 text-xs font-bold text-ink-muted">
      {label}
      <select
        name={name}
        defaultValue={defaultValue || ""}
        className="focus-ring min-h-11 w-full rounded-lg border border-line bg-surface px-3 text-sm font-medium text-ink"
      >
        <option value="">{emptyLabel}</option>
        {children}
      </select>
    </label>
  );
}
