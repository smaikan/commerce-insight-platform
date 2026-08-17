import "server-only";

import { cache } from "react";

import { apiGet } from "@/lib/api/client";
import type {
  BrandPage,
  CatalogClassifications,
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
export const getCatalogClassifications = cache(async (): Promise<CatalogClassifications> => {
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

// Burada filtre seçeneklerini ürün liste sorgusuyla aynı bağlamdaki self-excluding published facet uçlarından alıyorum.
export async function getPublishedProductFacets(query: PublishedProductQuery): Promise<CatalogFacets> {
  const search = new URLSearchParams();
  if (query.TypeId) search.set("TypeId", query.TypeId);
  if (query.BrandId) search.set("BrandId", query.BrandId);
  if (query.CollectionId) search.set("CollectionId", query.CollectionId);
  const suffix = search.size ? `?${search.toString()}` : "";

  const [brands, collections, productTypes] = await Promise.all([
    apiGet<CatalogFacets["brands"]>(`/api/products/published/facets/brands${suffix}`, {
      revalidate: 30,
      tags: ["products", "published-product-facets"],
    }),
    apiGet<CatalogFacets["collections"]>(`/api/products/published/facets/collections${suffix}`, {
      revalidate: 30,
      tags: ["products", "published-product-facets"],
    }),
    apiGet<CatalogFacets["productTypes"]>(`/api/products/published/facets/product-types${suffix}`, {
      revalidate: 30,
      tags: ["products", "published-product-facets"],
    }),
  ]);

  return { brands, collections, productTypes };
}
