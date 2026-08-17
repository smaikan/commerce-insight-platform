import "server-only";

import { cache } from "react";

import type { StorefrontNavigationGroup, StorefrontNavigationItem } from "@/components/storefront/navigation-types";
import { apiGet } from "@/lib/api/client";
import { classificationSegmentFromName } from "@/modules/catalog/classification-url";
import type { components } from "@/generated/api";

type PublishedFacetItem = components["schemas"]["PublishedProductFacetItemDto"];

const NAVIGATION_FACET_CONFIG = [
  {
    id: "categories",
    label: "Kategoriler",
    href: "/categories",
    path: "/api/products/published/facets/product-types",
    routePrefix: "/category",
    tag: "published-product-type-facets",
  },
  {
    id: "collections",
    label: "Koleksiyonlar",
    href: "/collections",
    path: "/api/products/published/facets/collections",
    routePrefix: "/collection",
    tag: "published-collection-facets",
  },
  {
    id: "brands",
    label: "Markalar",
    path: "/api/products/published/facets/brands",
    routePrefix: "/brand",
    tag: "published-brand-facets",
  },
] as const;

// Burada global navigasyon facetlerini paralel, paylaşımlı cache ve grup bazlı hata izolasyonuyla hazırlıyorum.
export const getStorefrontNavigation = cache(async (): Promise<StorefrontNavigationGroup[]> => {
  const groups = await Promise.all(
    NAVIGATION_FACET_CONFIG.map(async (config): Promise<StorefrontNavigationGroup> => {
      try {
        const facets = await apiGet<PublishedFacetItem[]>(config.path, {
          revalidate: 300,
          tags: ["published-products", config.tag],
        });

        return {
          id: config.id,
          label: config.label,
          href: "href" in config ? config.href : undefined,
          items: navigationItems(facets, config.routePrefix),
        };
      } catch {
        return { id: config.id, label: config.label, href: "href" in config ? config.href : undefined, items: [] };
      }
    }),
  );

  return groups;
});

// Burada yalnız sonuç üreten ve adı çakışmasız biçimde URL'ye dönüştürülebilen facetleri navigasyona alıyorum.
function navigationItems(facets: PublishedFacetItem[], routePrefix: string): StorefrontNavigationItem[] {
  const candidates = facets
    .filter((facet) => facet.productCount > 0)
    .map((facet) => ({ facet, segment: classificationSegmentFromName(facet.name) }))
    .filter((candidate) => candidate.segment.length > 0);
  const segmentCounts = new Map<string, number>();

  for (const candidate of candidates) {
    segmentCounts.set(candidate.segment, (segmentCounts.get(candidate.segment) || 0) + 1);
  }

  return candidates
    .filter((candidate) => segmentCounts.get(candidate.segment) === 1)
    .map(({ facet, segment }) => ({
      id: facet.id,
      label: facet.name,
      href: `${routePrefix}/${encodeURIComponent(segment)}`,
      productCount: facet.productCount,
    }))
    .sort((left, right) => left.label.localeCompare(right.label, "tr"));
}
