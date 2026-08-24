import "server-only";

import { cache } from "react";

import { apiGet } from "@/lib/api/client";
import type { ProductSeoData, ProductSeoIndexItem, ProductSeoIndexPage } from "@/modules/product/types";

// Burada metadata ve sayfanın aynı yayınlanmış ürün isteğini render kapsamında paylaşmasını sağlıyorum.
export const getPublishedProductBySlug = cache(async (slug: string): Promise<ProductSeoData> =>
  apiGet<ProductSeoData>(`/api/products/by-url/${encodeURIComponent(slug)}`, {
    cache: "no-store",
  }),
);

// Burada sitemap için yayınlanmış ürün URL'lerini API üst sınırına uygun sayfalar halinde topluyorum.
export async function getAllProductSeoIndex(): Promise<ProductSeoIndexItem[]> {
  const items: ProductSeoIndexItem[] = [];
  let pageNumber = 1;

  while (true) {
    const page = await apiGet<ProductSeoIndexPage>(
      `/api/products/seo-index?PageNumber=${pageNumber}&PageSize=100`,
      { revalidate: 300, tags: ["products", "product-seo-index"] },
    );
    items.push(...page.items);

    if (!page.hasNextPage) return items;
    pageNumber += 1;
  }
}
