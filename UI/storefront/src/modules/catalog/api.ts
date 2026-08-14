import "server-only";

import { cache } from "react";

import { apiGet } from "@/lib/api/client";
import type {
  BrandPage,
  CatalogFacets,
  CollectionPage,
  ProductTypePage,
  PublishedProductPage,
  PublishedProductQuery,
} from "@/modules/catalog/types";

// Burada OpenAPI sorgu alanlarını belgeli public katalog endpointine güvenli biçimde ekliyorum.
export async function getPublishedProducts(query: PublishedProductQuery): Promise<PublishedProductPage> {
  const search = new URLSearchParams();

  Object.entries(query).forEach(([key, value]) => {
    if (value !== undefined) search.set(key, String(value));
  });

  return apiGet<PublishedProductPage>(`/api/products/published?${search.toString()}`, {
    revalidate: 60,
    tags: ["products", "published-products"],
  });
}

// Burada katalog facet seçeneklerini bağımsız public endpointlerden paralel ve paylaşımlı cache ile alıyorum.
export const getCatalogFacets = cache(async (): Promise<CatalogFacets> => {
  const [brands, collections, productTypes] = await Promise.all([
    apiGet<BrandPage>("/api/brands?PageNumber=1&PageSize=100", {
      revalidate: 300,
      tags: ["brands"],
    }),
    apiGet<CollectionPage>("/api/collections?PageNumber=1&PageSize=100", {
      revalidate: 300,
      tags: ["collections"],
    }),
    apiGet<ProductTypePage>("/api/product-types?PageNumber=1&PageSize=100", {
      revalidate: 300,
      tags: ["product-types"],
    }),
  ]);

  return {
    brands: brands.items.filter((brand) => brand.isActive).sort((left, right) => left.name.localeCompare(right.name, "tr")),
    collections: collections.items
      .filter((collection) => collection.isActive)
      .sort((left, right) => left.displayOrder - right.displayOrder || left.name.localeCompare(right.name, "tr")),
    productTypes: productTypes.items
      .filter((productType) => productType.isActive)
      .sort((left, right) => left.name.localeCompare(right.name, "tr")),
  };
});
