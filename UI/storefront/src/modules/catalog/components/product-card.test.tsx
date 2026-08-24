import { renderToStaticMarkup } from "react-dom/server";
import { describe, expect, it, vi } from "vitest";

import type { PublishedProduct } from "@/modules/catalog/types";

import { ProductCard } from "./product-card";

vi.mock("next/navigation", () => ({ useRouter: () => ({ push: vi.fn(), refresh: vi.fn() }) }));
vi.mock("@/modules/auth/components/header-session", () => ({ useHeaderSession: () => "authenticated" }));

// Burada katalog kartı testinde yalnız generated published ürün sözleşmesinin gerçek alanlarını kullanıyorum.
const product: PublishedProduct = {
  id: "P00001",
  title: "İnci Kolye",
  url: "inci-kolye",
  summary: null,
  brandName: "SERANTIS",
  price: 1499,
  compareAtPrice: null,
  averageRating: 0,
  ratingCount: 0,
  mainImage: undefined,
  isAvailable: true,
  lowestAvailableStock: 4,
  isLowStock: false,
};

describe("product card favorites", () => {
  // Burada kart linkini bozmadan ayrı, erişilebilir ve basılabilir favori kontrolü bulunduğunu doğruluyorum.
  it("renders a favorite control over the product media", () => {
    const html = renderToStaticMarkup(<ProductCard product={product} />);

    expect(html).toContain('href="/products/inci-kolye"');
    expect(html).toContain('aria-label="İnci Kolye ürününü favorilere ekle"');
    expect(html).toContain('aria-pressed="false"');
    expect(html).toContain("size-11");
    expect(html).toContain("size-8");
    expect(html).toContain("rounded-xl");
    expect(html).not.toContain("border-line text-ink");
  });
});
