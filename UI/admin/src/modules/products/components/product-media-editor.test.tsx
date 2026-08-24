import { renderToStaticMarkup } from "react-dom/server";
import { describe, expect, it, vi } from "vitest";

vi.mock("next/navigation", () => ({ useRouter: () => ({ refresh: vi.fn() }) }));
vi.mock("@/lib/admin/components/confirm-dialog", () => ({ ConfirmDialog: () => null }));
vi.mock("@/modules/products/actions", () => ({ deleteProductImageAction: vi.fn() }));
vi.mock("@/modules/products/cloudinary-upload", () => ({ validateProductImageFile: vi.fn(() => null) }));
vi.mock("@/modules/products/product-media", () => import("../product-media"));

import { ProductMediaEditor } from "./product-media-editor";
import type { ProductImage } from "../types";

const images: ProductImage[] = [
  {
    id: "11111111-1111-4111-8111-111111111111",
    productId: "P00042",
    imageUrl: "https://example.com/zebra.webp",
    altText: "Zebra",
    displayOrder: 0,
    isMain: true,
  },
  {
    id: "22222222-2222-4222-8222-222222222222",
    productId: "P00042",
    imageUrl: "https://example.com/alpha.webp",
    altText: "Alpha",
    displayOrder: 1,
    isMain: false,
  },
];

describe("ProductMediaEditor ordering controls", () => {
  it("renders API display order rather than alphabetically sorting image labels", () => {
    const html = renderToStaticMarkup(
      <ProductMediaEditor
        productId="P00042"
        images={images}
        onDirty={vi.fn()}
        onDraftChange={vi.fn()}
      />,
    );

    expect(html.indexOf("Zebra, sıra 1/2")).toBeLessThan(html.indexOf("Alpha, sıra 2/2"));
    expect(html).toContain("Zebra görselini sonraki sıraya taşı");
    expect(html).toContain("Alpha görselini önceki sıraya taşı");
    expect(html).toContain("draggable=\"true\"");
  });
});
