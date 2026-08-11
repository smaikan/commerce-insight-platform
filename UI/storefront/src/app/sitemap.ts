import type { MetadataRoute } from "next";
import { productCanonicalUrl } from "@/lib/product-seo";
import { siteConfig } from "@/lib/site-config";
import { getAllProductSeoIndex } from "@/modules/product/api";

// Burada API build sırasında erişilebilir olmasa da sitemap'i çalışma anında otoriter katalogdan üretiyorum.
export const dynamic = "force-dynamic";

// Burada yalnız canonical ve yayınlanmış public URL'leri sitemap içine dahil ediyorum.
export default async function sitemap(): Promise<MetadataRoute.Sitemap> {
  const products = await getAllProductSeoIndex();
  return [
    {
      url: siteConfig.url,
      changeFrequency: "daily",
      priority: 1,
    },
    {
      url: `${siteConfig.url}/products`,
      changeFrequency: "daily",
      priority: 0.9,
    },
    ...products.map((product) => ({
      url: productCanonicalUrl(product.url),
      lastModified: new Date(product.lastModifiedAt),
      changeFrequency: "daily" as const,
      priority: 0.8,
    })),
  ];
}
