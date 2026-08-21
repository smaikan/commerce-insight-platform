import { renderToStaticMarkup } from "react-dom/server";
import { describe, expect, it } from "vitest";

import type { CategoryShowcasePage } from "@/modules/catalog/categories";

import { CategoryShowcase } from "./category-showcase";

// Burada testlerin gerçek sayfalı API modelini küçük içerik değişiklikleriyle yeniden kullanmasını sağlıyorum.
function showcasePage(overrides: Partial<CategoryShowcasePage> = {}): CategoryShowcasePage {
  return {
    items: [],
    pageNumber: 1,
    pageSize: 20,
    totalCount: 0,
    totalPages: 0,
    hasPreviousPage: false,
    hasNextPage: false,
    ...overrides,
  };
}

describe("category showcase", () => {
  // Burada backend'in özel ve fallback görsellerini, adetlerini ve kimlik tabanlı katalog bağlantılarını aynen sunduğumu doğruluyorum.
  it("renders backend-owned images, counts and TypeId filter links", () => {
    const html = renderToStaticMarkup(
      <CategoryShowcase page={showcasePage({
        items: [
          {
            id: "11111111-1111-4111-8111-111111111111",
            name: "Özel & Yüzükler",
            href: "/products?type=11111111-1111-4111-8111-111111111111",
            imageUrl: "https://res.cloudinary.com/demo/category-custom.jpg",
            imageAlt: "Özel & Yüzükler",
            productCount: 9,
          },
          {
            id: "22222222-2222-4222-8222-222222222222",
            name: "Kolyeler",
            href: "/products?type=22222222-2222-4222-8222-222222222222",
            imageUrl: "https://res.cloudinary.com/demo/backend-product-fallback.jpg",
            imageAlt: "Kolyeler",
            productCount: 4,
          },
        ],
        totalCount: 2,
        totalPages: 1,
      })} />,
    );

    expect(html).toContain("Kategoriler");
    expect(html).toContain("category-custom.jpg");
    expect(html).toContain("backend-product-fallback.jpg");
    expect(html).toContain('href="/products?type=11111111-1111-4111-8111-111111111111"');
    expect(html).not.toContain("ozel-yuzukler");
    expect(html).toContain("9 ürün");
    expect(html).toContain("2 kategori");
    expect(html).toContain("md:grid-cols-3");
    expect(html).toContain("aspect-[16/10]");
    expect(html).toContain("sm:aspect-[3/2]");
    expect(html).not.toContain("aspect-square");
  });

  // Burada null imageUrl için yerel ve erişilebilir placeholder gösterildiğini doğruluyorum.
  it("renders the local placeholder when imageUrl is null", () => {
    const html = renderToStaticMarkup(
      <CategoryShowcase page={showcasePage({
        items: [{
          id: "33333333-3333-4333-8333-333333333333",
          name: "Görselsiz",
          href: "/products?type=33333333-3333-4333-8333-333333333333",
          imageUrl: null,
          imageAlt: "Görselsiz",
          productCount: 1,
        }],
        totalCount: 1,
        totalPages: 1,
      })} />,
    );

    expect(html).toContain("Kategori görseli bulunmuyor");
    expect(html).not.toContain("/_next/image");
  });

  // Burada API boş döndüğünde kontrollü boş durumun ve katalog kurtarma bağlantısının sunulduğunu doğruluyorum.
  it("renders a controlled empty state", () => {
    const html = renderToStaticMarkup(<CategoryShowcase page={showcasePage()} />);

    expect(html).toContain("Henüz görüntülenecek kategori yok");
    expect(html).toContain('href="/products"');
  });

  // Burada önceki ve sonraki bağlantılarının backend sayfalama bilgilerini koruduğunu doğruluyorum.
  it("renders pagination links from the API response", () => {
    const html = renderToStaticMarkup(
      <CategoryShowcase page={showcasePage({
        pageNumber: 2,
        pageSize: 40,
        totalCount: 120,
        totalPages: 3,
        hasPreviousPage: true,
        hasNextPage: true,
      })} />,
    );

    expect(html).toContain('href="/categories?pageSize=40"');
    expect(html).toContain('href="/categories?page=3&amp;pageSize=40"');
    expect(html).toContain("2</span> / <span");
  });
});
