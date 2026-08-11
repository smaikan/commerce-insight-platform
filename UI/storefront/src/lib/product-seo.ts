import type { Metadata } from "next";

import { siteConfig } from "./site-config";

export type ProductSeoShape = {
  product: {
    title: string;
    mainSku: string;
    description?: string | null;
    url: string;
    brandName?: string | null;
    seoTitle?: string | null;
    seoDescription?: string | null;
    averageRating: number;
    ratingCount: number;
    variants: Array<{
      sku: string;
      name: string;
      value: string;
      price: number;
      stock: number;
      isActive: boolean;
    }>;
  };
  images: Array<{
    imageUrl: string;
    altText?: string | null;
  }>;
};

// Burada API slug değerini değiştirmeden mutlak canonical ürün URL'si oluşturuyorum.
export function productCanonicalUrl(slug: string): string {
  return `${siteConfig.url}/products/${encodeURIComponent(slug)}`;
}

// Burada SEO açıklamasını görünür ürün içeriğinden üretip güvenli ve kısa bir metne normalleştiriyorum.
export function productDescription(product: ProductSeoShape["product"]): string {
  const source = product.seoDescription || product.description || `${product.title} ürününü inceleyin.`;
  const normalized = source.replace(/<[^>]*>/g, " ").replace(/\s+/g, " ").trim();
  return normalized.length <= 160 ? normalized : `${normalized.slice(0, 157).trimEnd()}...`;
}

// Burada ürün metadata değerlerini sayfadaki aynı otoriter ürün ve görsel verisinden oluşturuyorum.
export function buildProductMetadata(data: ProductSeoShape): Metadata {
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
      locale: "tr_TR",
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

// Burada yalnız görünür ve doğrulanmış ürün alanlarını Product yapılandırılmış verisine taşıyorum.
export function buildProductJsonLd(data: ProductSeoShape) {
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
    ...(product.brandName ? { brand: { "@type": "Brand", name: product.brandName } } : {}),
    ...(product.ratingCount > 0
      ? {
          aggregateRating: {
            "@type": "AggregateRating",
            ratingValue: product.averageRating,
            ratingCount: product.ratingCount,
          },
        }
      : {}),
    // Burada boş bir offers dizisi yayımlamak yerine teklif alanını yalnız gerçek aktif varyant varsa ekliyorum.
    ...(activeVariants.length > 0
      ? {
          offers: activeVariants.map((variant) => ({
            "@type": "Offer",
            sku: variant.sku,
            name: [variant.name, variant.value].filter(Boolean).join(" - "),
            url: canonical,
            price: variant.price,
            priceCurrency: siteConfig.currency,
            availability: variant.stock > 0 ? "https://schema.org/InStock" : "https://schema.org/OutOfStock",
          })),
        }
      : {}),
  };
}

// Burada görünür Ürünler > ürün hiyerarşisini aynı canonical URL'lerle BreadcrumbList verisine dönüştürüyorum.
export function buildProductBreadcrumbJsonLd(data: ProductSeoShape) {
  const canonical = productCanonicalUrl(data.product.url);

  return {
    "@context": "https://schema.org",
    "@type": "BreadcrumbList",
    itemListElement: [
      {
        "@type": "ListItem",
        position: 1,
        name: "Ana sayfa",
        item: siteConfig.url,
      },
      {
        "@type": "ListItem",
        position: 2,
        name: "Ürünler",
        item: `${siteConfig.url}/products`,
      },
      {
        "@type": "ListItem",
        position: 3,
        name: data.product.title,
        item: canonical,
      },
    ],
  };
}

// Burada JSON-LD serileştirmesinde script kapanışı üretebilecek karakterleri kaçırıyorum.
export function serializeJsonLd(value: unknown): string {
  return JSON.stringify(value).replace(/</g, "\\u003c");
}
