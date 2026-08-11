import { describe, expect, it } from "vitest";

import {
  buildProductBreadcrumbJsonLd,
  buildProductJsonLd,
  buildProductMetadata,
  productCanonicalUrl,
  serializeJsonLd,
  type ProductSeoShape,
} from "./product-seo";

const productData: ProductSeoShape = {
  product: {
    title: "Siyah Spor Ayakkabı",
    mainSku: "SHOE-MAIN",
    description: "Günlük kullanıma uygun spor ayakkabı.",
    url: "siyah-spor-ayakkabi",
    brandName: "Example",
    seoTitle: null,
    seoDescription: null,
    averageRating: 4.5,
    ratingCount: 10,
    variants: [
      {
        name: "Numara",
        value: "42",
        sku: "SHOE-42",
        price: 1299,
        stock: 5,
        isActive: true,
      },
    ],
  },
  images: [
    {
      imageUrl: "https://cdn.example.com/shoe.jpg",
      altText: "Siyah Spor Ayakkabı",
    },
  ],
};

describe("product SEO", () => {
  // Burada ürün başlığı, açıklaması ve API slug değerinin metadata içinde korunduğunu doğruluyorum.
  it("uses product fallbacks and the canonical product URL", () => {
    const metadata = buildProductMetadata(productData);

    expect(metadata.title).toBe("Siyah Spor Ayakkabı");
    expect(metadata.description).toBe("Günlük kullanıma uygun spor ayakkabı.");
    expect(metadata.alternates?.canonical).toBe(productCanonicalUrl("siyah-spor-ayakkabi"));
  });

  // Burada görünür ürün hiyerarşisinin mutlak canonical URL'lerle BreadcrumbList olarak üretildiğini doğruluyorum.
  it("builds the visible product breadcrumb hierarchy", () => {
    const breadcrumb = buildProductBreadcrumbJsonLd(productData);

    expect(breadcrumb.itemListElement).toHaveLength(3);
    expect(breadcrumb.itemListElement[2]).toMatchObject({
      name: "Siyah Spor Ayakkabı",
      item: productCanonicalUrl("siyah-spor-ayakkabi"),
    });
  });

  // Burada Product JSON-LD'nin gerçek puan, teklif, para birimi ve stok durumunu taşıdığını doğruluyorum.
  it("builds Product JSON-LD with rating, offer, currency and availability", () => {
    const jsonLd = buildProductJsonLd(productData);

    expect(jsonLd["@type"]).toBe("Product");
    expect(jsonLd.aggregateRating).toMatchObject({ ratingValue: 4.5, ratingCount: 10 });
    expect(jsonLd.offers).toEqual([
      expect.objectContaining({
        sku: "SHOE-42",
        price: 1299,
        priceCurrency: "TRY",
        availability: "https://schema.org/InStock",
      }),
    ]);
  });

  // Burada JSON-LD içinde script kapanışı üretebilecek karakterlerin kaçırıldığını doğruluyorum.
  it("escapes markup-significant characters in serialized JSON-LD", () => {
    expect(serializeJsonLd({ name: "</script><script>" })).not.toContain("</script>");
    expect(serializeJsonLd({ name: "</script><script>" })).toContain("\\u003c");
  });
});
