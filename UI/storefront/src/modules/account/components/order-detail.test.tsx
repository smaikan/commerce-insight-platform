import { renderToStaticMarkup } from "react-dom/server";
import { describe, expect, it, vi } from "vitest";

import type { AccountOrder } from "@/modules/account/contracts";
import { OrderDetail } from "@/modules/account/components/order-detail";

vi.mock("next/navigation", () => ({ useRouter: () => ({ refresh: vi.fn() }) }));

const order: AccountOrder = {
  id: "bb49d4c3-9752-4116-9179-657c8d6259b0",
  orderNumber: "ORD-2026-1",
  status: 4,
  subTotal: 1200,
  discountTotal: 0,
  shippingTotal: 50,
  taxTotal: 0,
  grandTotal: 1250,
  couponCode: null,
  shippingMethodName: "Standart teslimat",
  items: [{
    id: "8d52d55c-1acd-4c54-a9a0-3354e9f0d263",
    productId: "P00001",
    productVariantId: "a71e05d8-d9ce-4351-88f2-1b52580ae39e",
    productTitle: "Test yüzüğü",
    variantSku: "SKU-1",
    variantName: "Renk",
    variantValue: "Pudra",
    unitPrice: 1200,
    quantity: 1,
    totalPrice: 1200,
    discountTotal: 0,
    taxTotal: 0,
    refundTotal: 1200,
    taxRatePercentage: 0,
    productUrl: "test-yuzugu",
    imageUrl: null,
    imageAlt: null,
  }],
  payments: [],
  shippingAddress: {
    sourceAddressId: null,
    title: "Ev",
    firstName: "Test",
    lastName: "Müşteri",
    phoneNumber: "05000000000",
    city: "İstanbul",
    district: "Kadıköy",
    fullAddress: "Test adresi",
    postalCode: null,
  },
  billingAddress: undefined,
  customer: undefined,
  reservationExpiresAt: null,
  paidAt: null,
  cancelledAt: null,
  createdAt: "2026-08-13T08:00:00Z",
  shippingCarrier: "Test Kargo",
  trackingNumber: "TRK123",
  trackingUrl: "https://cargo.example/track/TRK123",
  shippedAt: "2026-08-14T08:00:00Z",
  deliveredAt: "2026-08-15T08:00:00Z",
};

describe("account order detail", () => {
  // Burada güvenli takip bağlantısı ile gerçek kargoya çıkış ve teslim tarihlerinin doğru sırada sunulduğunu doğruluyorum.
  it("renders safe tracking and shipment dates in chronological order", () => {
    const html = renderToStaticMarkup(<OrderDetail order={order} />);
    expect(html).toContain("Renk: Pudra");
    expect(html).toContain('href="https://cargo.example/track/TRK123"');
    expect(html).toContain('rel="noreferrer"');
    expect(html.indexOf("Kargoya verildi")).toBeLessThan(html.indexOf("Teslim edildi"));
    expect(html).toContain("İade ve değişim işlemleri");
    expect(html).not.toContain("Siparişi iptal et");
  });

  // Burada ödemesi alınmış fakat henüz kargoya verilmemiş siparişte iptal aksiyonunun görünür kaldığını doğruluyorum.
  it("offers cancellation for a paid pre-shipment order", () => {
    const html = renderToStaticMarkup(<OrderDetail order={{ ...order, status: 2, shippedAt: null, deliveredAt: null }} />);
    expect(html).toContain("Siparişi iptal et");
    expect(html).not.toContain("İade ve değişim işlemleri");
  });

  // Burada URL bulunmadığında takip numarasını gösterip harici takip aksiyonu üretmediğimi doğruluyorum.
  it("shows a tracking number without inventing a tracking link", () => {
    const html = renderToStaticMarkup(<OrderDetail order={{ ...order, trackingUrl: null }} />);
    expect(html).toContain("TRK123");
    expect(html).not.toContain("Kargoyu takip et");
  });
});
