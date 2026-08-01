import { cache } from "react";
import { siteConfig } from "./site-config";

export type ProductVariant = {
  id: string;
  name: string;
  value: string;
  sku: string;
  price: number;
  compareAtPrice: number | null;
  stock: number;
  isActive: boolean;
};

export type ProductImage = {
  id: string;
  imageUrl: string;
  altText: string | null;
  displayOrder: number;
  isMain: boolean;
};

export type Product = {
  id: string;
  title: string;
  mainSku: string;
  description: string | null;
  url: string;
  brandName: string | null;
  seoTitle: string | null;
  seoDescription: string | null;
  averageRating: number;
  ratingCount: number;
  variants: ProductVariant[];
};

export type ProductSeoResponse = {
  product: Product;
  images: ProductImage[];
  lastModifiedAt: string;
};

type SeoIndexItem = {
  url: string;
  lastModifiedAt: string;
};

type PagedResult<T> = {
  items: T[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasNextPage: boolean;
};

async function apiFetch<T>(path: string): Promise<T | null> {
  const response = await fetch(`${siteConfig.apiUrl}${path}`, {
    headers: { Accept: "application/json" },
    next: { revalidate: 60 },
  });

  if (response.status === 404) {
    return null;
  }

  if (!response.ok) {
    throw new Error(`API request failed with status ${response.status}.`);
  }

  return (await response.json()) as T;
}

export const getProductBySlug = cache(async (slug: string) =>
  apiFetch<ProductSeoResponse>(`/api/products/by-url/${encodeURIComponent(slug)}`),
);

export async function getAllProductSeoIndex(): Promise<SeoIndexItem[]> {
  const items: SeoIndexItem[] = [];
  let pageNumber = 1;

  while (true) {
    const page = await apiFetch<PagedResult<SeoIndexItem>>(
      `/api/products/seo-index?pageNumber=${pageNumber}&pageSize=100`,
    );

    if (!page) {
      return items;
    }

    items.push(...page.items);
    if (!page.hasNextPage) {
      return items;
    }

    pageNumber += 1;
  }
}
