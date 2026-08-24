import Link from "next/link";

import { CatalogFilters } from "@/modules/catalog/components/catalog-filters";
import { CatalogPagination } from "@/modules/catalog/components/catalog-pagination";
import { CatalogToolbar } from "@/modules/catalog/components/catalog-toolbar";
import { ProductCard } from "@/modules/catalog/components/product-card";
import { catalogHref, type CatalogUrlOptions } from "@/modules/catalog/query";
import type { CatalogFacets, CatalogView, PublishedProductPage } from "@/modules/catalog/types";

type CatalogPageLayoutProps = {
  eyebrow?: string;
  title: string;
  description?: string;
  products: PublishedProductPage;
  facets: CatalogFacets;
  view: CatalogView;
  urlOptions?: CatalogUrlOptions;
  emptyDescription: string;
};

// Burada ana katalog ile marka, koleksiyon ve kategori sayfalarının aynı lüks editoryal hiyerarşi ve ürün gridini paylaşmasını sağlıyorum.
export function CatalogPageLayout({
  eyebrow,
  title,
  description,
  products,
  facets,
  view,
  urlOptions,
  emptyDescription,
}: CatalogPageLayoutProps) {
  const hasRefinements = Boolean(
    (view.brandId && urlOptions?.omitFilter !== "brandId") ||
      (view.collectionId && urlOptions?.omitFilter !== "collectionId") ||
      (view.typeId && urlOptions?.omitFilter !== "typeId"),
  );

  return (
    <main id="main-content" className="page-shell max-w-[84rem] flex-1 py-6 sm:py-8 lg:py-10">
      {/* Breadcrumb Gezintisi */}
      <nav aria-label="Sayfa yolu" className="mb-4 flex items-center gap-1.5 text-xs text-ink-muted">
        <Link href="/" className="focus-ring hover:text-brand-700 transition-colors">
          Ana Sayfa
        </Link>
        <span aria-hidden="true" className="text-line">/</span>
        {eyebrow ? (
          <>
            <Link
              href={eyebrow === "Kategori" ? "/categories" : eyebrow === "Koleksiyon" ? "/collections" : "/products"}
              className="focus-ring hover:text-brand-700 transition-colors"
            >
              {eyebrow === "Kategori" ? "Kategoriler" : eyebrow === "Koleksiyon" ? "Koleksiyonlar" : "Katalog"}
            </Link>
            <span aria-hidden="true" className="text-line">/</span>
            <span aria-current="page" className="font-semibold text-ink truncate max-w-[14rem]">
              {title}
            </span>
          </>
        ) : title.toLowerCase() === "tüm ürünler" || title.toLowerCase() === "katalog" ? (
          <span aria-current="page" className="font-semibold text-ink truncate max-w-[14rem]">
            Katalog
          </span>
        ) : (
          <>
            <Link href="/products" className="focus-ring hover:text-brand-700 transition-colors">
              Katalog
            </Link>
            <span aria-hidden="true" className="text-line">/</span>
            <span aria-current="page" className="font-semibold text-ink truncate max-w-[14rem]">
              {title}
            </span>
          </>
        )}
      </nav>

      {/* Lüks Başlık ve Açıklama Alanı */}
      <header className="border-b border-line/70 pb-6 sm:pb-8">
        {eyebrow ? (
          <p className="mb-2 text-xs font-bold tracking-[0.2em] text-brand-700 uppercase">
            {eyebrow}
          </p>
        ) : null}
        <h1 className="text-3xl font-bold tracking-tight text-ink sm:text-4xl lg:text-5xl">
          {title}
        </h1>
        {description ? (
          <p className="mt-3 max-w-2xl text-sm leading-relaxed text-ink-muted sm:text-base">
            {description}
          </p>
        ) : null}

        {/* Hızlı Kategori Filtre Hapları (Ana Katalogda) */}
        {!urlOptions?.omitFilter && facets.productTypes.length > 0 ? (
          <div className="mt-6 -mx-4 px-4 sm:-mx-0 sm:px-0 flex items-center gap-2 overflow-x-auto pb-1 scrollbar-none">
            <Link
              href={catalogHref({ ...view, page: 1, typeId: undefined }, urlOptions)}
              prefetch={false}
              className={`focus-ring shrink-0 rounded-xl px-3.5 py-1.5 text-xs font-semibold transition-all ${
                !view.typeId
                  ? "bg-brand-950 text-white shadow-xs"
                  : "bg-surface-subtle border border-line text-ink-muted hover:border-brand-700 hover:text-ink"
              }`}
            >
              Tüm Ürünler
            </Link>
            {facets.productTypes.map((type) => {
              const isSelected = view.typeId === type.id;
              return (
                <Link
                  key={type.id}
                  href={catalogHref({ ...view, page: 1, typeId: isSelected ? undefined : type.id }, urlOptions)}
                  prefetch={false}
                  className={`focus-ring shrink-0 rounded-xl px-3.5 py-1.5 text-xs font-semibold transition-all ${
                    isSelected
                      ? "bg-brand-950 text-white shadow-xs"
                      : "bg-surface-subtle border border-line text-ink-muted hover:border-brand-700 hover:text-ink"
                  }`}
                >
                  {type.name} ({type.productCount})
                </Link>
              );
            })}
          </div>
        ) : null}
      </header>

      {/* Filtre ve Toolbar Alanı */}
      <CatalogFilters facets={facets} view={view} urlOptions={urlOptions} />
      <CatalogToolbar view={view} totalCount={products.totalCount} urlOptions={urlOptions} />

      {/* Ürün Izgarası */}
      {products.items.length > 0 ? (
        <section
          className="mt-6 sm:mt-8 grid grid-cols-2 gap-x-3 gap-y-5 sm:gap-x-6 sm:gap-y-10 md:grid-cols-3 lg:grid-cols-4 lg:gap-x-6 lg:gap-y-12"
          aria-label={`${title} ürünleri`}
        >
          {products.items.map((product, index) => (
            <ProductCard key={product.id} product={product} isLcpCandidate={index < 4} />
          ))}
        </section>
      ) : (
        <section className="mt-12 rounded-2xl border border-line/80 bg-surface px-6 py-16 text-center shadow-xs">
          <div className="mx-auto flex size-14 items-center justify-center rounded-2xl bg-surface-subtle text-brand-700 mb-4">
            <svg aria-hidden="true" viewBox="0 0 24 24" className="size-7" fill="none" stroke="currentColor" strokeWidth="1.5">
              <circle cx="11" cy="11" r="8" />
              <path d="m21 21-4.3-4.3" />
            </svg>
          </div>
          <h2 className="text-xl font-bold text-ink">Gösterilecek ürün bulunamadı</h2>
          <p className="mx-auto mt-2 max-w-md text-sm leading-relaxed text-ink-muted">
            {hasRefinements ? "Seçtiğiniz filtrelere uygun ürün bulunamadı. Filtreleri temizleyerek tüm modelleri inceleyebilirsiniz." : emptyDescription}
          </p>
          {hasRefinements ? (
            <Link
              className="focus-ring mt-6 inline-flex items-center gap-2 rounded-xl bg-brand-950 px-6 py-3 text-xs font-bold text-white shadow-xs hover:bg-brand-700 transition-colors"
              href={urlOptions?.basePath || "/products"}
            >
              <span>Filtreleri Temizle</span>
              <span aria-hidden="true">&rarr;</span>
            </Link>
          ) : null}
        </section>
      )}

      {/* Lüks Sayfalama */}
      <CatalogPagination
        page={products.pageNumber}
        totalPages={products.totalPages}
        view={view}
        urlOptions={urlOptions}
      />
    </main>
  );
}
