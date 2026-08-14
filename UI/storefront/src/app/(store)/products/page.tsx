import type { Metadata } from "next";
import { notFound, redirect } from "next/navigation";

import { getCatalogFacets, getPublishedProducts } from "@/modules/catalog/api";
import { CatalogPageLayout } from "@/modules/catalog/components/catalog-page-layout";
import {
  catalogCanonicalHref,
  catalogHref,
  catalogSearchParamsNeedRedirect,
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
  // Burada metadata akışından önce ileri sayfanın varlığını doğrulayıp not-found ve noindex sinyalini erkenden üretiyorum.
  if (view.page > 1) {
    const products = await getPublishedProducts(toPublishedProductQuery(view));
    if (products.items.length === 0) notFound();
  }
  const isFiltered = hasCatalogFilters(view);
  const shouldNoIndex = Boolean(view.search) || isFiltered || view.sort !== "newest";
  const canonical = view.sort !== "newest" ? catalogCanonicalHref(view) : catalogHref(view);
  // Burada görünür katalog başlığıyla metadata amacını aynı tutup tanıtım dili yerine gerçek filtreleme kapsamını açıklıyorum.
  const baseTitle = view.search ? `“${view.search}” için arama sonuçları` : "Tüm ürünler";
  const title = view.page > 1 ? `${baseTitle} · Sayfa ${view.page}` : baseTitle;
  const description = view.search
    ? `“${view.search}” aramasıyla eşleşen yayımdaki ürünleri inceleyin.`
    : "Yayımdaki ürünleri marka, koleksiyon ve kategoriye göre inceleyin.";

  return {
    title,
    description,
    alternates: { canonical },
    robots: shouldNoIndex ? { index: false, follow: true } : { index: true, follow: true },
    openGraph: {
      type: "website",
      url: canonical,
      title,
      description,
    },
  };
}

// Burada katalog verisini sunucuda alıp crawl edilebilir ürün gridini ilk HTML içinde oluşturuyorum.
export default async function ProductsPage({ searchParams }: ProductsPageProps) {
  const rawSearchParams = await searchParams;
  const view = parseCatalogView(rawSearchParams);
  // Burada filtre formundan gelen boş/default parametreleri veri isteğinden önce temiz ve paylaşılabilir katalog URL'sine indiriyorum.
  if (catalogSearchParamsNeedRedirect(rawSearchParams, view)) redirect(catalogHref(view));
  const [products, facets] = await Promise.all([
    getPublishedProducts(toPublishedProductQuery(view)),
    getCatalogFacets(),
  ]);
  // Burada boş ve var olmayan ileri sayfaların indexlenebilir soft-404 üretmesini engelliyorum.
  if (view.page > 1 && products.items.length === 0) notFound();
  return (
    <CatalogPageLayout
      title={view.search ? `“${view.search}” için sonuçlar` : "Tüm ürünler"}
      products={products}
      facets={facets}
      view={view}
      emptyDescription={view.search ? "Bu aramayla eşleşen yayımlanmış ürün bulunamadı." : "Yeni ürünler yayınlandığında burada görünecek."}
    />
  );
}
