import { renderToStaticMarkup } from "react-dom/server";
import { describe, expect, it, vi } from "vitest";

vi.mock("next/navigation", () => ({ useRouter: () => ({ push: vi.fn(), refresh: vi.fn() }) }));

import { ProductSummary } from "./product-summary";
import type { Product } from "../types";

const mockProduct: Product = {
  id: "00000000-0000-0000-0000-000000000001",
  title: "Zarif Altın Yüzük",
  slug: "zarif-altin-yuzuk",
  description: "Özel tasarım altın yüzük.",
  brandName: "ELEVEN",
  categoryName: "Yüzük",
  hasVariants: true,
  variants: [
    {
      id: "var-1",
      name: "Ölçü",
      value: "12",
      price: 1299,
      stock: 5,
      isActive: true,
      material: "14 Ayar Altın",
    },
    {
      id: "var-2",
      name: "Ölçü",
      value: "14",
      price: 1499,
      stock: 3,
      isActive: true,
      material: "14 Ayar Altın",
    },
  ],
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
