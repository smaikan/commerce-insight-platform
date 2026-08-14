import { renderToStaticMarkup } from "react-dom/server";
import { describe, expect, it } from "vitest";

import type { CollectionShowcasePage } from "@/modules/catalog/collections";

import { CollectionShowcase } from "./collection-showcase";

// Burada testlerin yalnız içerik değiştirerek aynı gerçek sayfalı API modelini kullanmasını sağlıyorum.
function showcasePage(overrides: Partial<CollectionShowcasePage> = {}): CollectionShowcasePage {
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

describe("collection showcase", () => {
  // Burada doğrudan API görseli, ürün adedi ve backend URL'si kullanılırken öğe sırasının korunmasını doğruluyorum.
  it("renders backend-owned collection data in backend order", () => {
    const html = renderToStaticMarkup(
      <CollectionShowcase page={showcasePage({
        items: [
          {
            id: "z-last-alphabetically",
            name: "Zümrüt Seçkisi",
            url: "not-used-in-view",
            href: "/collection/backend-owned-path",
            imageUrl: "https://res.cloudinary.com/demo/showcase.jpg",
            imageAlt: "Zümrüt Seçkisi",
            productCount: 7,
            isFeatured: true,
            displayOrder: 1,
          },
          {
            id: "a-first-alphabetically",
            name: "Ada",
            url: "not-used-in-view-either",
            href: "/collection/ada-api-url",
            imageUrl: null,
            imageAlt: "Ada",
            productCount: 2,
            isFeatured: false,
            displayOrder: 2,
          },
        ],
        totalCount: 2,
        totalPages: 1,
      })} />,
    );

    expect(html).toContain("Koleksiyonlar");
    expect(html).toContain('href="/collection/backend-owned-path"');
    expect(html).not.toContain("zumrut-seckisi");
    expect(html).toContain('alt="Zümrüt Seçkisi"');
    expect(html).toContain("7 ürün");
    expect(html).toContain("2 koleksiyon");
    expect(html.indexOf("Zümrüt Seçkisi")).toBeLessThan(html.indexOf(">Ada</h2>"));
  });

  // Burada null imageUrl için erişilebilir placeholder gösterildiğini ve istemciye sahte görsel kaynağı verilmediğini doğruluyorum.
  it("renders the accessible placeholder when imageUrl is null", () => {
    const html = renderToStaticMarkup(
      <CollectionShowcase page={showcasePage({
        items: [{
          id: "missing-image",
          name: "Sade",
          url: "sade",
          href: "/collection/sade",
          imageUrl: null,
          imageAlt: "Sade",
          productCount: 1,
          isFeatured: false,
          displayOrder: 1,
        }],
        totalCount: 1,
        totalPages: 1,
      })} />,
    );

    expect(html).toContain("Koleksiyon görseli bulunmuyor");
    expect(html).not.toContain("/_next/image");
  });

  // Burada önceki ve sonraki sayfa bağlantılarının API'den dönen sayfa bilgisiyle pageSize değerini koruduğunu doğruluyorum.
  it("renders pagination links from the paged response", () => {
    const html = renderToStaticMarkup(
      <CollectionShowcase page={showcasePage({
        pageNumber: 2,
        pageSize: 40,
        totalCount: 120,
        totalPages: 3,
        hasPreviousPage: true,
        hasNextPage: true,
      })} />,
    );

    expect(html).toContain('href="/collections?pageSize=40"');
    expect(html).toContain('href="/collections?page=3&amp;pageSize=40"');
    expect(html).toContain("2</span> / 3");
  });
});
