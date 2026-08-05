import type { components } from "@/generated/api";

export type Product = components["schemas"]["ProductDto"];
export type ProductVariant = components["schemas"]["ProductVariantDto"];
export type ProductImage = components["schemas"]["ProductImageDto"];
export type Brand = components["schemas"]["BrandDto"];
export type ProductType = components["schemas"]["ProductTypeDto"];
export type TaxRate = components["schemas"]["TaxRateDto"];
export type ProductStatus = components["schemas"]["ProductStatus"];
export type ProductSortBy = components["schemas"]["ProductSortBy"];

// Burada OpenAPI'nin ürün liste response şeması eksik olduğu için ortak sayfalama gövdesini belgelenen alanlarla sınırlıyorum.
export type PagedResult<T> = {
  items: T[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
};

export type ProductListQuery = {
  pageNumber: number;
  pageSize: number;
  search?: string;
  typeId?: string;
  brandId?: string;
  status?: ProductStatus;
  isFeatured?: boolean;
  sortBy: ProductSortBy;
  descending: boolean;
};

export type ProductFormOptions = {
  brands: Brand[];
  taxRates: TaxRate[];
  taxRatesUnavailable: boolean;
};

export type ProductActionState = {
  status: "idle" | "error" | "partial";
  message?: string;
  traceId?: string;
  productId?: string;
  reloadHref?: string;
  fieldErrors?: Record<string, string[]>;
};

export const initialProductActionState: ProductActionState = { status: "idle" };
