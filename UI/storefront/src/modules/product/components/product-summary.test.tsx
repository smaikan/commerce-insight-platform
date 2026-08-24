import { renderToStaticMarkup } from "react-dom/server";
import { describe, expect, it, vi } from "vitest";

vi.mock("next/navigation", () => ({ useRouter: () => ({ push: vi.fn(), refresh: vi.fn() }) }));

import { ProductSummary } from "./product-summary";
import type { Product } from "../types";

const mockProduct: Product = {
  id: "00000000-0000-0000-0000-000000000001",
  title: "Zarif Altın Yüzük",
  mainSku: "YUZUK",
  description: "Özel tasarım altın yüzük.",
  url: "zarif-altin-yuzuk",
  brandName: "ELEVEN",
  status: 1,
  isActive: true,
  isFeatured: false,
  hasVariants: true,
  displayOrder: 0,
  clickCount: 0,
  totalAddToCartCount: 0,
  totalPurchaseCount: 0,
  favoriteCount: 0,
  popularityScore: 0,
  averageRating: 0,
  ratingCount: 0,
  reviewCount: 0,
  variants: [
    {
      id: "var-1",
      productId: "00000000-0000-0000-0000-000000000001",
      name: "Ölçü",
      value: "12",
      sku: "YUZUK-12",
      price: 1299,
      netPrice: 1299,
      stock: 5,
      addToCartCount: 0,
      purchaseCount: 0,
      isActive: true,
      material: "14 Ayar Altın",
      concurrencyToken: "00000000-0000-0000-0000-000000000011",
    },
    {
      id: "var-2",
      productId: "00000000-0000-0000-0000-000000000001",
      name: "Ölçü",
      value: "14",
      sku: "YUZUK-14",
      price: 1499,
      netPrice: 1499,
      stock: 3,
      addToCartCount: 0,
      purchaseCount: 0,
      isActive: true,
      material: "14 Ayar Altın",
      concurrencyToken: "00000000-0000-0000-0000-000000000012",
    },
  ],
  tags: [],
  collections: [],
  images: [],
};

describe("product summary", () => {
  it("renders the selected initial variant price", () => {
    const html = renderToStaticMarkup(<ProductSummary product={mockProduct} />);

    expect(html).toContain("1.299,00");
    expect(html).toContain("Zarif Altın Yüzük");
    expect(html).toContain("ELEVEN");
  });
});
