import { renderToStaticMarkup } from "react-dom/server";
import { describe, expect, it } from "vitest";

import type { ProductImage } from "@/modules/product/types";

import { orderProductImages, ProductGallery } from "./product-gallery";

const images: ProductImage[] = [
  { id: "second", productId: "product", imageUrl: "https://res.cloudinary.com/demo/image/upload/second.jpg", altText: "İkinci", isMain: false, displayOrder: 2 },
  { id: "main", productId: "product", imageUrl: "https://res.cloudinary.com/demo/image/upload/main.jpg", altText: "Ana", isMain: true, displayOrder: 5 },
  { id: "third", productId: "product", imageUrl: "https://res.cloudinary.com/demo/image/upload/third.jpg", altText: null, isMain: false, displayOrder: 3 },
];

describe("product gallery", () => {
  // Burada ana görselin displayOrder'dan bağımsız ilk sıraya, diğerlerinin kendi sırasına geldiğini doğruluyorum.
  it("orders main and secondary product images deterministically", () => {
    expect(orderProductImages(images).map((image) => image.id)).toEqual(["main", "second", "third"]);
  });

  // Burada üç görselli mobil carousel'in tüm slaytları, 4:5 geometrisini ve yalnız ana görsel önceliğini koruduğunu doğruluyorum.
  it("renders a responsive three-image carousel without eagerly loading secondary media", () => {
    const html = renderToStaticMarkup(<ProductGallery images={orderProductImages(images)} productTitle="Test ürün" />);

    expect(html.match(/data-carousel-slide="true"/g)).toHaveLength(3);
    expect(html.match(/aspect-\[4\/5\]/g)).toHaveLength(3);
    expect(html.match(/rel="preload" as="image"/g)).toHaveLength(1);
    expect(html).toMatch(/rel="preload"[^>]+main\.jpg/);
    expect(html.match(/loading="lazy"/g)).toHaveLength(2);
    expect(html).toContain('aria-label="3 / 3"');
  });
});
