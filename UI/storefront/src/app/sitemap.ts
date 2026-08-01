import type { MetadataRoute } from "next";
import { getAllProductSeoIndex } from "@/lib/products-api";
import { productCanonicalUrl } from "@/lib/product-seo";
import { siteConfig } from "@/lib/site-config";

export const dynamic = "force-dynamic";

export default async function sitemap(): Promise<MetadataRoute.Sitemap> {
  const products = await getAllProductSeoIndex();
  return [
    {
      url: siteConfig.url,
      changeFrequency: "daily",
      priority: 1,
    },
    ...products.map((product) => ({
      url: productCanonicalUrl(product.url),
      lastModified: new Date(product.lastModifiedAt),
      changeFrequency: "daily" as const,
      priority: 0.8,
    })),
  ];
}
