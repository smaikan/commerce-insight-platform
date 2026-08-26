import type { components } from "@/generated/api";
export type { PagedResult } from "@/lib/api/pagination";

export type Product = components["schemas"]["ProductDto"];
export type ProductVariant = components["schemas"]["ProductVariantDto"];
export type ProductImage = components["schemas"]["ProductImageDto"];
export type Brand = components["schemas"]["BrandDto"];
export type ProductType = components["schemas"]["ProductTypeDto"];
export type TaxRate = components["schemas"]["TaxRateDto"];
export type Collection = components["schemas"]["CollectionDto"];
export type Tag = components["schemas"]["TagDto"];
export type ProductStatus = components["schemas"]["ProductStatus"];
export type ProductSortBy = components["schemas"]["ProductSortBy"];

// Burada ürün listesinin desteklediği belgeli URL filtrelerini taşıyorum.
export type ProductListQuery = {
  pageNumber: number;
  pageSize: number;
  search?: string;
  typeId?: string;
  brandId?: string;
  collectionId?: string;
  tagId?: string;
  status?: ProductStatus;
  isFeatured?: boolean;
  sortBy: ProductSortBy;
  descending: boolean;
};

// Burada ürün listesi filtre çubuğu için gerekli referans seçeneklerini tanımlıyorum.
export type ProductListOptions = {
  productTypes: ProductType[];
  brands: Brand[];
  collections: Collection[];
  tags: Tag[];
};

export type ProductFormOptions = {
  brands: Brand[];
  taxRates: TaxRate[];
  collections: Collection[];
  taxRatesUnavailable: boolean;
  collectionsUnavailable: boolean;
};

export type ProductActionState = {
  status: "idle" | "success" | "error" | "partial";
  message?: string;
  traceId?: string;
  productId?: string;
  completionToken?: string;
  reloadHref?: string;
  fieldErrors?: Record<string, string[]>;
  completedOperations?: string[];
  failedOperations?: string[];
  savedVariantEditorState?: {
    mainSku: string;
    hasVariants: boolean;
    variants: ProductVariant[];
  };
};

export const initialProductActionState: ProductActionState = { status: "idle" };
