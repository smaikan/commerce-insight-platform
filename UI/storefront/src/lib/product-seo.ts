import type { Metadata } from "next";
import type { ProductSeoResponse } from "./products-api";
import { siteConfig } from "./site-config";

export function productCanonicalUrl(slug: string): string {
  return `${siteConfig.url}/products/${encodeURIComponent(slug)}`;
}

export function productDescription(product: ProductSeoResponse["product"]): string {
  const source = product.seoDescription || product.description || `${product.title} ürününü inceleyin.`;
  const normalized = source.replace(/<[^>]*>/g, " ").replace(/\s+/g, " ").trim();
  return normalized.length <= 160 ? normalized : `${normalized.slice(0, 157).trimEnd()}...`;
}

export function buildProductMetadata(data: ProductSeoResponse): Metadata {
  const { product, images } = data;
  const title = product.seoTitle?.trim() || product.title;
  const description = productDescription(product);
  const canonical = productCanonicalUrl(product.url);
  const socialImages = images.map((image) => ({
    url: image.imageUrl,
    alt: image.altText || product.title,
  }));

  return {
    title,
    description,
    alternates: { canonical },
    openGraph: {
      type: "website",
      url: canonical,
      title,
      description,
      siteName: siteConfig.name,
      images: socialImages,
    },
    twitter: {
      card: socialImages.length > 0 ? "summary_large_image" : "summary",
      title,
      description,
      images: socialImages.map((image) => image.url),
    },
  };
}

export function buildProductJsonLd(data: ProductSeoResponse) {
  const { product, images } = data;
  const canonical = productCanonicalUrl(product.url);
  const activeVariants = product.variants.filter((variant) => variant.isActive);

  return {
    "@context": "https://schema.org",
    "@type": "Product",
    name: product.title,
    description: productDescription(product),
    sku: product.mainSku,
    url: canonical,
    image: images.map((image) => image.imageUrl),
    ...(product.brandName
      ? { brand: { "@type": "Brand", name: product.brandName } }
      : {}),
    ...(product.ratingCount > 0
      ? {
          aggregateRating: {
            "@type": "AggregateRating",
            ratingValue: product.averageRating,
            ratingCount: product.ratingCount,
          },
        }
      : {}),
    offers: activeVariants.map((variant) => ({
      "@type": "Offer",
      sku: variant.sku,
      name: [variant.name, variant.value].filter(Boolean).join(" - "),
      url: canonical,
      price: variant.price,
      priceCurrency: siteConfig.currency,
      availability:
        variant.stock > 0
          ? "https://schema.org/InStock"
          : "https://schema.org/OutOfStock",
      itemCondition: "https://schema.org/NewCondition",
    })),
  };
}

export function serializeJsonLd(value: unknown): string {
  return JSON.stringify(value).replace(/</g, "\\u003c");
}
