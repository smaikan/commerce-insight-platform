import type { Metadata } from "next";
import Link from "next/link";

import { getCatalogFacets, getPublishedProducts } from "@/modules/catalog/api";
import { CatalogFilters } from "@/modules/catalog/components/catalog-filters";
import { CatalogPagination } from "@/modules/catalog/components/catalog-pagination";
import { CatalogToolbar } from "@/modules/catalog/components/catalog-toolbar";
import { ProductCard } from "@/modules/catalog/components/product-card";
import {
  catalogCanonicalHref,
  catalogHref,
  hasCatalogFilters,
  parseCatalogView,
  toPublishedProductQuery,
} from "@/modules/catalog/query";
import type { CatalogSearchParams } from "@/modules/catalog/types";

type ProductsPageProps = {
  searchParams: Promise<CatalogSearchParams>;
};

// Burada sayfalama için kendine ait canonical, sıralama kopyaları için temiz canonical ve noindex kararı üretiyorum.
export async function generateMetadata({ searchParams }: ProductsPageProps): Promise<Metadata> {
  const view = parseCatalogView(await searchParams);
  const isFiltered = hasCatalogFilters(view);
  const shouldNoIndex = isFiltered || view.sort !== "newest";
  const canonical = isFiltered ? catalogCanonicalHref(view) : view.sort !== "newest" ? "/products" : catalogHref(view);

  return {
    title: view.page > 1 ? `Ürünler · Sayfa ${view.page}` : "Ürünler",
    description: "Yayındaki ürünleri inceleyin; yeni, popüler veya önerilen sıraya göre keşfedin.",
    alternates: { canonical },
    robots: shouldNoIndex ? { index: false, follow: true } : { index: true, follow: true },
    openGraph: {
      type: "website",
      url: canonical,
      title: view.page > 1 ? `Ürünler · Sayfa ${view.page}` : "Ürünler",
      description: "Yayındaki ürünleri inceleyin; yeni, popüler veya önerilen sıraya göre keşfedin.",
    },
  };
}

// Burada katalog verisini sunucuda alıp crawl edilebilir ürün gridini ilk HTML içinde oluşturuyorum.
export default async function ProductsPage({ searchParams }: ProductsPageProps) {
  const view = parseCatalogView(await searchParams);
  const [products, facets] = await Promise.all([
    getPublishedProducts(toPublishedProductQuery(view)),
    getCatalogFacets(),
  ]);
  const hasFilters = hasCatalogFilters(view);

  return (
    <main id="main-content" className="page-shell max-w-[80rem] flex-1 py-8 sm:py-12 lg:py-14">
      <header className="max-w-2xl pb-7 sm:pb-9">
        <p className="mb-3 text-xs font-bold tracking-[0.14em] text-brand-700 uppercase">Mağaza</p>
        <h1 className="text-3xl font-semibold tracking-[-0.035em] text-ink sm:text-4xl">Ürünleri keşfet</h1>
        <p className="mt-4 max-w-xl text-sm leading-6 text-ink-muted sm:text-base">
          Güncel ürünleri sade bir katalogda inceleyin ve size uygun seçeneğe hızlıca ulaşın.
        </p>
      </header>

      <CatalogFilters facets={facets} view={view} />
      <CatalogToolbar view={view} totalCount={products.totalCount} />

      {products.items.length > 0 ? (
        <section className="mt-7 grid grid-cols-2 gap-x-3 gap-y-8 sm:gap-x-4 md:grid-cols-3 lg:grid-cols-4 lg:gap-x-6" aria-label="Ürün listesi">
          {products.items.map((product) => <ProductCard key={product.id} product={product} />)}
        </section>
      ) : (
        <section className="mt-10 rounded-xl border border-line bg-surface px-6 py-14 text-center">
          <h2 className="text-lg font-semibold text-ink">Gösterilecek ürün bulunamadı</h2>
          <p className="mt-2 text-sm text-ink-muted">
            {hasFilters ? "Seçtiğiniz filtrelere uygun ürün bulunamadı." : "Yeni ürünler yayınlandığında burada görünecek."}
          </p>
          {hasFilters ? (
            <Link className="focus-ring mt-5 inline-block text-sm font-bold text-brand-700 hover:text-brand-950" href="/products">
              Tüm ürünleri göster
            </Link>
          ) : null}
        </section>
      )}

      <CatalogPagination page={products.pageNumber} totalPages={products.totalPages} view={view} />
    </main>
  );
}
