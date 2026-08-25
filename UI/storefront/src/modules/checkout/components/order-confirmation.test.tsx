import { renderToStaticMarkup } from "react-dom/server";
import { describe, expect, it } from "vitest";

import type { GuestOrder } from "@/modules/checkout/types";
import { CancellationCompletedNotice, GuestOrderItems } from "@/modules/checkout/components/order-confirmation";
import { OrderCancellationControl } from "@/modules/checkout/components/order-cancellation-control";

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

describe("order confirmation cancellation actions", () => {
  it("labels the cancellation action without promising a cart redirect", () => {
    const html = renderToStaticMarkup(
      <OrderCancellationControl
        orderId="bb49d4c3-9752-4116-9179-657c8d6259b0"
        orderStatus={1}
        accessMode="guest"
      />,
    );

    expect(html).toContain("Siparişi iptal et");
    expect(html).not.toContain("Siparişi iptal et ve sepete dön");
  });

  it("does not offer a cart action after cancellation completes", () => {
    const html = renderToStaticMarkup(<CancellationCompletedNotice paidOrderWasReversed={false} />);

    expect(html).toContain("Sipariş iptal edildi");
    expect(html).not.toContain("Sepete dön");
    expect(html).not.toContain('href="/checkout"');
  });
});
