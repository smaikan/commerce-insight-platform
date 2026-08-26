import { describe, expect, it } from "vitest";
import {
  productFormIdentity,
  reconcileProductFormIdentity,
  type ProductFormIdentity,
} from "./product-edit-revision";

describe("product edit form revision", () => {
  it("keeps the mounted form identity while a variant and media draft is being saved", () => {
    const mounted: ProductFormIdentity = { productId: "P00042", revision: "before" };
    const revalidated: ProductFormIdentity = { productId: "P00042", revision: "after" };

    expect(reconcileProductFormIdentity(mounted, revalidated, true)).toBe(mounted);
  });

  it("adopts the authoritative revision after the complete save clears the draft", () => {
    const mounted: ProductFormIdentity = { productId: "P00042", revision: "before" };
    const revalidated: ProductFormIdentity = { productId: "P00042", revision: "after" };

    expect(reconcileProductFormIdentity(mounted, revalidated, false)).toEqual(revalidated);
  });

  it("never carries a draft identity to another product", () => {
    const mounted: ProductFormIdentity = { productId: "P00042", revision: "before" };
    const nextProduct: ProductFormIdentity = { productId: "P00043", revision: "initial" };

    expect(reconcileProductFormIdentity(mounted, nextProduct, true)).toEqual(nextProduct);
  });

  it("changes the server revision when an editable variant changes", () => {
    const product = {
      id: "P00042",
      status: 1 as const,
      hasVariants: true,
      mainSku: "MAIN-42",
      variants: [{
        id: "11111111-1111-4111-8111-111111111111",
        productId: "P00042",
        name: "Renk",
        value: "Mavi",
        sku: "SKU-MAVI",
        price: 100,
        netPrice: 83.33,
        stock: 2,
        addToCartCount: 0,
        purchaseCount: 0,
        isActive: true,
        concurrencyToken: "22222222-2222-4222-8222-222222222222",
      }],
    };

    const before = productFormIdentity(product);
    const after = productFormIdentity({
      ...product,
      variants: [{ ...product.variants[0], price: 120 }],
    });

    expect(after.revision).not.toBe(before.revision);
  });
});
