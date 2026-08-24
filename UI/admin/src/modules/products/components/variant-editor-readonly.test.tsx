import { renderToStaticMarkup } from "react-dom/server";
import { describe, expect, it, vi } from "vitest";

vi.mock("@/modules/products/actions", () => ({ deleteProductVariantAction: vi.fn() }));
vi.mock("@/lib/admin/components/confirm-dialog", () => ({ ConfirmDialog: () => null }));
vi.mock("@/modules/products/variant-editing", () => import("../variant-editing"));
vi.mock("@/modules/products/variant-combinations", () => import("../variant-combinations"));

import { VariantEditor } from "./variant-editor";
import type { ProductVariant } from "../types";

const persistedVariant: ProductVariant = {
  id: "11111111-1111-4111-8111-111111111111",
  productId: "P00042",
  name: "Beden",
  value: "M",
  variantOptionNameId: null,
  variantOptionValueId: null,
  sku: "SKU-M",
  barcode: null,
  material: null,
  price: 899.9,
  netPrice: 749.9,
  compareAtPrice: null,
  stock: 12,
  addToCartCount: 0,
  purchaseCount: 0,
  isActive: true,
  concurrencyToken: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa",
};

describe("variant editor stock fields", () => {
  it("renders persisted variant stock as read-only", () => {
    const html = renderToStaticMarkup(
      <VariantEditor
        variants={[persistedVariant]}
        mode="edit"
        productId="P00042"
        initialHasVariants
        initialMainSku="MAIN-SKU"
      />,
    );

    expect(html).toContain("Mevcut stok");
    const stockInput = html.match(/<input[^>]*name="variants\.0\.stock"[^>]*>/)?.[0];
    expect(stockInput).toContain("readOnly=\"\"");
  });

  it("keeps opening stock editable for a new product variant", () => {
    const html = renderToStaticMarkup(
      <VariantEditor
        variants={[]}
        mode="create"
        initialHasVariants={false}
        initialMainSku="NEW-SKU"
      />,
    );

    expect(html).toContain("Açılış stoğu");
    const stockInput = html.match(/<input[^>]*name="variants\.0\.stock"[^>]*>/)?.[0];
    expect(stockInput).toBeDefined();
    expect(stockInput).not.toContain("readOnly=\"\"");
  });
});
