import { describe, expect, it } from "vitest";
import type { ProductSeoResponse } from "./products-api";
import {
  buildProductJsonLd,
  buildProductMetadata,
  productCanonicalUrl,
  serializeJsonLd,
} from "./product-seo";

const productData: ProductSeoResponse = {
  product: {
    id: "P00001",
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
        id: "variant-1",
        name: "Numara",
        value: "42",
        sku: "SHOE-42",
        price: 1299,
        compareAtPrice: null,
        stock: 5,
        isActive: true,
      },
    ],
  },
  images: [
    {
      id: "image-1",
      imageUrl: "https://cdn.example.com/shoe.jpg",
      altText: "Siyah Spor Ayakkabı",
      displayOrder: 0,
      isMain: true,
    },
  ],
  lastModifiedAt: "2026-08-01T00:00:00Z",
};

describe("product SEO", () => {
  it("uses product fallbacks and the canonical product URL", () => {
    const metadata = buildProductMetadata(productData);

    expect(metadata.title).toBe("Siyah Spor Ayakkabı");
    expect(metadata.description).toBe("Günlük kullanıma uygun spor ayakkabı.");
    expect(metadata.alternates?.canonical).toBe(productCanonicalUrl("siyah-spor-ayakkabi"));
  });

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

  it("escapes markup-significant characters in serialized JSON-LD", () => {
    expect(serializeJsonLd({ name: "</script><script>" })).not.toContain("</script>");
    expect(serializeJsonLd({ name: "</script><script>" })).toContain("\\u003c");
  });
});
