import type { components } from "@/generated/api";

export type ProductSeoData = components["schemas"]["ProductSeoDto"];
export type Product = components["schemas"]["ProductDto"];
export type ProductImage = components["schemas"]["ProductImageDto"];
export type ProductVariant = components["schemas"]["ProductVariantDto"];

// OpenAPI seo-index response içeriğini henüz üretmediği için bu dar geçici tipi endpointin belgeli alanlarıyla tutuyorum.
export type ProductSeoIndexItem = {
  url: string;
  lastModifiedAt: string;
};

export type ProductSeoIndexPage = {
  items: ProductSeoIndexItem[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasNextPage: boolean;
};
