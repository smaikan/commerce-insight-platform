import { describe, expect, it } from "vitest";
import { toOrderListPreview } from "./preview";
import type { Order } from "./types";

// Burada hızlı görünüm eşleyicisinin yalnız ihtiyaç duyulan müşteri, adres ve ürün alanlarını taşıdığını doğruluyorum.
describe("toOrderListPreview", () => {
  it("tam sipariş DTO'sunu daraltılmış liste özetine dönüştürür", () => {
    const order: Order = {
      id: "11111111-1111-1111-1111-111111111111",
      orderNumber: "ORD-1001",
      status: 2,
      subTotal: 1200,
      discountTotal: 100,
      shippingTotal: 50,
      taxTotal: 220,
      grandTotal: 1370,
      createdAt: "2026-08-03T12:00:00Z",
      customer: {
        firstName: "Ayşe",
        lastName: "Yılmaz",
        email: "ayse@example.com",
        phoneNumber: "+90 555 000 00 00",
      },
      shippingAddress: {
        title: "Ev",
        firstName: "Ayşe",
        lastName: "Yılmaz",
        phoneNumber: "+90 555 000 00 00",
        city: "İstanbul",
        district: "Kadıköy",
        fullAddress: "Örnek Mahallesi 1. Sokak No: 2",
        postalCode: "34000",
      },
      items: [
        {
          id: "22222222-2222-2222-2222-222222222222",
          productId: "P00004",
          productVariantId: "33333333-3333-3333-3333-333333333333",
          productTitle: "Uzun Kollu Gömlek",
          variantSku: "GOM-MAVI-M",
          unitPrice: 1200,
          quantity: 1,
          totalPrice: 1200,
          discountTotal: 0,
          taxTotal: 200,
          refundTotal: 0,
        },
      ],
      payments: [],
    };

    const preview = toOrderListPreview(order);

    expect(preview.customer).toEqual(order.customer);
    expect(preview.shippingAddress).toEqual(order.shippingAddress);
    expect(preview.items).toEqual([
      {
        id: order.items[0].id,
        productId: "P00004",
        productTitle: "Uzun Kollu Gömlek",
        variantSku: "GOM-MAVI-M",
        quantity: 1,
        totalPrice: 1200,
      },
    ]);
    expect(preview).not.toHaveProperty("payments");
    expect(preview).not.toHaveProperty("billingAddress");
  });
});
