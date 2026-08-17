import type { components, paths } from "@/generated/api";

export type PublishedProduct = components["schemas"]["PublishedProductListItemDto"];
export type PublishedProductPage = components["schemas"]["PublishedProductListItemDtoPagedResult"];
export type PublishedProductQuery = NonNullable<
  paths["/api/products/published"]["get"]["parameters"]["query"]
>;

export type Brand = components["schemas"]["BrandDto"];
export type BrandPage = components["schemas"]["BrandDtoPagedResult"];
export type Collection = components["schemas"]["CollectionDto"];
export type CollectionPage = components["schemas"]["CollectionDtoPagedResult"];
export type ProductType = components["schemas"]["ProductTypeDto"];
export type PublishedProductFacet = components["schemas"]["PublishedProductFacetItemDto"];

// OpenAPI product-types liste response body'sini henüz üretmediği için runtime'da doğrulanan ortak sayfalama biçimini dar tutuyorum.
export type ProductTypePage = {
  items: ProductType[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
};

export type CatalogClassifications = {
  brands: Brand[];
  collections: Collection[];
  productTypes: ProductType[];
};

export type CatalogFacets = {
  brands: PublishedProductFacet[];
  collections: PublishedProductFacet[];
  productTypes: PublishedProductFacet[];
};

export type CatalogSort = "newest" | "popular" | "display-order" | "title";

export type CatalogSearchParams = Record<string, string | string[] | undefined>;

export type CatalogView = {
  page: number;
  sort: CatalogSort;
  hasExplicitSort?: boolean;
  search?: string;
  brandId?: string;
  collectionId?: string;
  typeId?: string;
};
