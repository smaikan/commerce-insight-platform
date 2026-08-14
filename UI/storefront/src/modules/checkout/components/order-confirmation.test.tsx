import { renderToStaticMarkup } from "react-dom/server";
import { describe, expect, it } from "vitest";

import type { GuestOrder } from "@/modules/checkout/types";
import { GuestOrderItems } from "@/modules/checkout/components/order-confirmation";

// Burada guest sipariş bileşenini generated OrderItemDto snapshot alanlarıyla besliyorum.
const item: GuestOrder["items"][number] = {
  id: "8d52d55c-1acd-4c54-a9a0-3354e9f0d263",
  productId: "P00001",
  productVariantId: "a71e05d8-d9ce-4351-88f2-1b52580ae39e",
  productTitle: "Pudra yüzük",
  variantSku: "SKU-PUDRA",
  variantName: "Renk",
  variantValue: "Pudra",
  unitPrice: 1200,
  quantity: 1,
  totalPrice: 1200,
  discountTotal: 0,
  taxTotal: 0,
  refundTotal: 1200,
  taxRatePercentage: 0,
  productUrl: "pudra-yuzuk",
  imageUrl: null,
  imageAlt: null,
};

describe("guest order item snapshots", () => {
  // Burada guest sipariş detayında checkout anındaki ad/değer snapshot'ının birlikte gösterildiğini doğruluyorum.
  it("renders the immutable variant snapshot", () => {
    const html = renderToStaticMarkup(<GuestOrderItems items={[item]} currency="TRY" />);
    expect(html).toContain("Renk: Pudra");
    expect(html).not.toContain("SKU-PUDRA");
  });

  // Burada null snapshot alanlarının guest detayında teknik fallback veya bozuk ayraç üretmediğini doğruluyorum.
  it("hides null variant snapshots without technical fallbacks", () => {
    const html = renderToStaticMarkup(<GuestOrderItems items={[{ ...item, variantName: null, variantValue: null, variantSku: "Varsayılan" }]} currency="TRY" />);
    expect(html).not.toContain("Varsayılan");
    expect(html).not.toContain("undefined");
    expect(html).not.toContain("Renk:");
  });
});
