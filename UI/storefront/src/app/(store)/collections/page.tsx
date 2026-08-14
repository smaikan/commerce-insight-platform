import type { Metadata } from "next";
import { notFound, redirect } from "next/navigation";

import { siteConfig } from "@/lib/site-config";
import { CollectionShowcase } from "@/modules/catalog/components/collection-showcase";
import { getCollectionShowcase } from "@/modules/catalog/collections";
import {
  COLLECTIONS_DEFAULT_PAGE_SIZE,
  collectionsHref,
  collectionsSearchParamsNeedRedirect,
  parseCollectionsView,
  type CollectionsSearchParams,
} from "@/modules/catalog/collections-query";

type CollectionsPageProps = {
  searchParams: Promise<CollectionsSearchParams>;
};

// Burada her koleksiyon sayfasına kendine ait canonical verirken özel pageSize görünümünü index dışında tutuyorum.
export async function generateMetadata({ searchParams }: CollectionsPageProps): Promise<Metadata> {
  const view = parseCollectionsView(await searchParams);
  const canonical = collectionsHref(view);
  const title = view.page > 1 ? `Koleksiyonlar · Sayfa ${view.page}` : "Koleksiyonlar";
  const description = `${siteConfig.name} koleksiyonlarını ve her koleksiyondaki yayımlanmış ürünleri inceleyin.`;

  return {
    title,
    description,
    alternates: { canonical },
    robots: view.pageSize === COLLECTIONS_DEFAULT_PAGE_SIZE
      ? { index: true, follow: true }
      : { index: false, follow: true },
    openGraph: { type: "website", url: canonical, title, description },
  };
}

// Burada URL sayfalamasını doğrulayıp tek server-only koleksiyon isteğiyle crawl edilebilir vitrini oluşturuyorum.
export default async function CollectionsPage({ searchParams }: CollectionsPageProps) {
  const rawSearchParams = await searchParams;
  const view = parseCollectionsView(rawSearchParams);
  if (collectionsSearchParamsNeedRedirect(rawSearchParams, view)) redirect(collectionsHref(view));

  const collections = await getCollectionShowcase(view.page, view.pageSize);
  if (view.page > 1 && collections.items.length === 0) notFound();

  return <CollectionShowcase page={collections} />;
}
