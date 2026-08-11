import { describe, expect, it } from "vitest";

import {
  parseAddCartItemRequest,
  parseCartConcurrencyRequest,
  parseUpdateCartItemRequest,
} from "./request";

const variantId = "05a5e045-0935-4d7d-b7f1-7e00def70c93";
const concurrencyToken = "21ee9c6b-f6d2-4aae-9672-44ed04eda316";

describe("cart request", () => {
  // Burada beklenmeyen fiyat ve ürün alanlarının güven sınırını geçmeden atıldığını doğruluyorum.
  it("keeps only the allowlisted add-item fields", () => {
    expect(parseAddCartItemRequest({
      productVariantId: variantId,
      quantity: 1,
      expectedConcurrencyToken: concurrencyToken,
      price: 1,
      productId: "P0001D",
    })).toEqual({ productVariantId: variantId, quantity: 1, expectedConcurrencyToken: concurrencyToken });
  });

  // Burada geçersiz varyant ve adet değerlerinin upstream API'ye ulaşmadığını doğruluyorum.
  it("rejects malformed add-item requests", () => {
    expect(parseAddCartItemRequest({ productVariantId: "bad", quantity: 1 })).toBeNull();
    expect(parseAddCartItemRequest({ productVariantId: variantId, quantity: 0 })).toBeNull();
  });

  // Burada adet güncellemesinde fazladan browser alanlarının atıldığını ve token'ın zorunlu kaldığını doğruluyorum.
  it("parses only valid update-item fields", () => {
    expect(parseUpdateCartItemRequest({
      quantity: 3,
      expectedConcurrencyToken: concurrencyToken,
      currentUnitPrice: 1,
    })).toEqual({ quantity: 3, expectedConcurrencyToken: concurrencyToken });
    expect(parseUpdateCartItemRequest({ quantity: 3 })).toBeNull();
    expect(parseUpdateCartItemRequest({ quantity: 1.5, expectedConcurrencyToken: concurrencyToken })).toBeNull();
  });

  // Burada silme isteklerinde yalnızca geçerli concurrency token'ın kabul edildiğini doğruluyorum.
  it("parses a valid concurrency request", () => {
    expect(parseCartConcurrencyRequest({ expectedConcurrencyToken: concurrencyToken, productId: "P0001D" }))
      .toEqual({ expectedConcurrencyToken: concurrencyToken });
    expect(parseCartConcurrencyRequest({ expectedConcurrencyToken: "bad" })).toBeNull();
  });
});
