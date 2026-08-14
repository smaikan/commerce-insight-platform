import Link from "next/link";

import { CatalogFilters } from "@/modules/catalog/components/catalog-filters";
import { CatalogPagination } from "@/modules/catalog/components/catalog-pagination";
import { CatalogToolbar } from "@/modules/catalog/components/catalog-toolbar";
import { ProductCard } from "@/modules/catalog/components/product-card";
import type { CatalogUrlOptions } from "@/modules/catalog/query";
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

// Burada ana katalog ile marka, koleksiyon ve kategori sayfalarının aynı görsel hiyerarşi ve ürün gridini paylaşmasını sağlıyorum.
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
    (view.brandId && urlOptions?.omitFilter !== "brandId")
    || (view.collectionId && urlOptions?.omitFilter !== "collectionId")
    || (view.typeId && urlOptions?.omitFilter !== "typeId"),
  );

  return (
    <main id="main-content" className="page-shell max-w-[80rem] flex-1 py-8 sm:py-12 lg:py-14">
      <header className="max-w-3xl pb-7 sm:pb-9">
        {/* Burada sınıflandırma türünü yalnız anlamlı olduğunda gösterip ana katalogda gereksiz üst etiketi kaldırıyorum. */}
        {eyebrow ? <p className="mb-2 text-xs font-bold tracking-[0.12em] text-brand-700 uppercase">{eyebrow}</p> : null}
        <h1 className="text-3xl font-semibold tracking-[-0.035em] text-brand-950 sm:text-4xl">{title}</h1>
        {/* Burada yalnız API'den gelen gerçek sınıflandırma açıklamasına yer ayırıp otomatik tanıtım cümlesi üretmiyorum. */}
        {description ? <p className="mt-3 max-w-2xl text-sm leading-6 text-ink-muted sm:text-base">{description}</p> : null}
      </header>

      <CatalogFilters facets={facets} view={view} urlOptions={urlOptions} />
      <CatalogToolbar view={view} totalCount={products.totalCount} urlOptions={urlOptions} />

      {products.items.length > 0 ? (
        <section className="mt-7 grid grid-cols-2 gap-x-3 gap-y-8 sm:gap-x-4 md:grid-cols-3 lg:grid-cols-4 lg:gap-x-6" aria-label={`${title} ürünleri`}>
          {products.items.map((product, index) => (
            <ProductCard key={product.id} product={product} isLcpCandidate={index === 0} />
          ))}
        </section>
      ) : (
        <section className="mt-10 rounded-xl border border-line bg-surface px-6 py-14 text-center">
          <h2 className="text-lg font-semibold text-ink">Gösterilecek ürün bulunamadı</h2>
          <p className="mt-2 text-sm text-ink-muted">
            {hasRefinements ? "Seçtiğiniz ek filtrelere uygun ürün bulunamadı." : emptyDescription}
          </p>
          {hasRefinements ? (
            <Link className="focus-ring mt-5 inline-block text-sm font-bold text-brand-700 hover:text-brand-950" href={urlOptions?.basePath || "/products"}>
              Ek filtreleri temizle
            </Link>
          ) : null}
        </section>
      )}

      <CatalogPagination
        page={products.pageNumber}
        totalPages={products.totalPages}
        view={view}
        urlOptions={urlOptions}
      />
    </main>
  );
}
