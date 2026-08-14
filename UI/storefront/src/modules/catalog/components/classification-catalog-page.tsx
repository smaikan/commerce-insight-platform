import type { Metadata } from "next";
import { notFound, permanentRedirect, redirect } from "next/navigation";
import { cache } from "react";

import { siteConfig } from "@/lib/site-config";
import { getCatalogFacets, getPublishedProducts } from "@/modules/catalog/api";
import {
  type CatalogClassificationKind,
  resolveCatalogClassification,
} from "@/modules/catalog/classification";
import { CatalogPageLayout } from "@/modules/catalog/components/catalog-page-layout";
import {
  catalogCanonicalHref,
  catalogHref,
  catalogSearchParamsNeedRedirect,
  hasCatalogFilters,
  parseCatalogView,
  toPublishedProductQuery,
  type CatalogUrlOptions,
} from "@/modules/catalog/query";
import type { CatalogSearchParams, CatalogView } from "@/modules/catalog/types";

type ClassificationPageInput = {
  kind: CatalogClassificationKind;
  segment: string;
  searchParams: CatalogSearchParams;
};

// Burada metadata ve sayfa render'ının aynı sınıflandırma, facet ve ürün sonucunu request kapsamında paylaşmasını sağlıyorum.
const getClassificationPageData = cache(async (
  kind: CatalogClassificationKind,
  segment: string,
  page: number,
  sort: CatalogView["sort"],
  brandId?: string,
  collectionId?: string,
  typeId?: string,
) => {
  const classification = await resolveCatalogClassification(kind, segment);
  if (!classification) return null;

  const view = withClassificationId(
    { page, sort, ...(brandId ? { brandId } : {}), ...(collectionId ? { collectionId } : {}), ...(typeId ? { typeId } : {}) },
    classification.filterKey,
    classification.id,
  );
  const [products, facets] = await Promise.all([
    getPublishedProducts(toPublishedProductQuery(view)),
    getCatalogFacets(),
  ]);

  return { classification, view, products, facets };
});

// Burada sınıflandırma sayfasının başlık, canonical ve index kararını görünür ürün kümesiyle aynı authoritative veriden üretiyorum.
export async function buildClassificationMetadata(input: ClassificationPageInput): Promise<Metadata> {
  const parsedView = parseCatalogView(input.searchParams);
  const data = await getClassificationPageData(
    input.kind,
    input.segment,
    parsedView.page,
    parsedView.sort,
    parsedView.brandId,
    parsedView.collectionId,
    parsedView.typeId,
  );
  if (!data) notFound();

  const urlOptions = classificationUrlOptions(data.classification.kind, data.classification.segment, data.classification.filterKey);
  const hasAdditionalFilters = hasVisibleFilters(data.view, data.classification.filterKey);
  const shouldNoIndex = hasAdditionalFilters || data.view.sort !== "newest";
  const canonical = data.view.sort !== "newest"
    ? catalogCanonicalHref(data.view, urlOptions)
    : catalogHref(data.view, urlOptions);
  const title = data.view.page > 1
    ? `${data.classification.name} · Sayfa ${data.view.page}`
    : data.classification.name;
  const description = classificationDescription(data.classification.name, data.classification.description);

  return {
    title: title === siteConfig.name ? { absolute: title } : title,
    description,
    alternates: { canonical },
    robots: shouldNoIndex ? { index: false, follow: true } : { index: true, follow: true },
    openGraph: { type: "website", url: canonical, title, description },
  };
}

// Burada üç dinamik route ailesini aynı katalog görünümünde birleştirip yalnız sınıflandırma ID'sini ürün sorgusunda değiştiriyorum.
export async function ClassificationCatalogPage(input: ClassificationPageInput) {
  const parsedView = parseCatalogView(input.searchParams);
  const data = await getClassificationPageData(
    input.kind,
    input.segment,
    parsedView.page,
    parsedView.sort,
    parsedView.brandId,
    parsedView.collectionId,
    parsedView.typeId,
  );
  if (!data) notFound();

  const urlOptions = classificationUrlOptions(data.classification.kind, data.classification.segment, data.classification.filterKey);
  if (input.segment !== data.classification.segment) permanentRedirect(urlOptions.basePath || "/products");
  if (catalogSearchParamsNeedRedirect(input.searchParams, data.view, urlOptions)) {
    redirect(catalogHref(data.view, urlOptions));
  }

  const hasAdditionalFilters = hasVisibleFilters(data.view, data.classification.filterKey);
  if (data.products.items.length === 0 && (data.view.page > 1 || !hasAdditionalFilters)) notFound();

  return (
    <CatalogPageLayout
      eyebrow={data.classification.eyebrow}
      title={data.classification.name}
      description={data.classification.description?.trim() || undefined}
      products={data.products}
      facets={data.facets}
      view={data.view}
      urlOptions={urlOptions}
      emptyDescription={data.classification.emptyDescription}
    />
  );
}

function classificationUrlOptions(
  kind: CatalogClassificationKind,
  segment: string,
  omitFilter: CatalogUrlOptions["omitFilter"],
): CatalogUrlOptions {
  return {
    basePath: `/${kind}/${encodeURIComponent(segment)}`,
    omitFilter,
  };
}

function withClassificationId(view: CatalogView, filterKey: NonNullable<CatalogUrlOptions["omitFilter"]>, id: string): CatalogView {
  return { ...view, [filterKey]: id };
}

function hasVisibleFilters(view: CatalogView, omittedFilter: NonNullable<CatalogUrlOptions["omitFilter"]>): boolean {
  return hasCatalogFilters({
    ...view,
    ...(omittedFilter === "brandId" ? { brandId: undefined } : {}),
    ...(omittedFilter === "collectionId" ? { collectionId: undefined } : {}),
    ...(omittedFilter === "typeId" ? { typeId: undefined } : {}),
  });
}

function classificationDescription(name: string, description: string | null | undefined): string {
  return description?.trim() || `${name} ürünlerini keşfedin ve size uygun seçenekleri inceleyin.`;
}
