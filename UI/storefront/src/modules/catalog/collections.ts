import "server-only";

import { cache } from "react";

import type { components, paths } from "@/generated/api";
import { apiGet } from "@/lib/api/client";
import { catalogSegmentFromApiUrl } from "@/modules/catalog/classification-url";
import { selectMostPopulated } from "@/modules/catalog/showcase-ranking";

type PublishedCollectionPage = components["schemas"]["PublishedCollectionShowcaseItemDtoPagedResult"];
type PublishedCollectionQuery = NonNullable<
  paths["/api/collections/published"]["get"]["parameters"]["query"]
>;

export type CollectionShowcaseItem = components["schemas"]["PublishedCollectionShowcaseItemDto"] & {
  href: string;
  imageAlt: string;
};

export type CollectionShowcasePage = Omit<PublishedCollectionPage, "items"> & {
  items: CollectionShowcaseItem[];
};

// Burada koleksiyon vitrinini ürün/facet join'i yapmadan tek public endpointten ve backend sırasını koruyarak alıyorum.
export const getCollectionShowcase = cache(async (
  pageNumber: number,
  pageSize: number,
): Promise<CollectionShowcasePage> => {
  const query: PublishedCollectionQuery = {
    PageNumber: pageNumber,
    PageSize: pageSize,
  };
  const search = new URLSearchParams();
  search.set("PageNumber", String(query.PageNumber));
  search.set("PageSize", String(query.PageSize));

  const page = await apiGet<PublishedCollectionPage>(
    `/api/collections/published?${search.toString()}`,
    { revalidate: 30, tags: ["published-collections"] },
  );

  return {
    ...page,
    // Burada API öğelerini yeniden sıralamadan yalnız route ve erişilebilir görsel metniyle sunum modeline taşıyorum.
    items: page.items.map((collection) => ({
      ...collection,
      href: `/collection/${encodeURIComponent(catalogSegmentFromApiUrl(collection.url))}`,
      imageAlt: collection.name,
    })),
  };
});

// Burada manuel koleksiyon sırasından bağımsız olarak tüm sayfaları kapsayıp görünür ürün sayısı en yüksek iki koleksiyonu seçiyorum.
export const getMostPopulatedCollections = cache(async (limit = 2): Promise<CollectionShowcaseItem[]> => {
  const firstPage = await getCollectionShowcase(1, 100);
  const remainingPages = firstPage.totalPages > 1
    ? await Promise.all(
      Array.from({ length: firstPage.totalPages - 1 }, (_, index) => getCollectionShowcase(index + 2, 100)),
    )
    : [];

  return selectMostPopulated(
    [firstPage, ...remainingPages].flatMap((page) => page.items),
    limit,
  );
});
