import { renderToStaticMarkup } from "react-dom/server";
import { describe, expect, it } from "vitest";

import type { components } from "@/generated/api";
import { OrderItemMedia } from "@/modules/account/components/order-item-media";

type OrderItem = components["schemas"]["OrderItemDto"];

const baseItem: OrderItem = {
  id: "8d52d55c-1acd-4c54-a9a0-3354e9f0d263",
  productId: "P00001",
  productVariantId: "a71e05d8-d9ce-4351-88f2-1b52580ae39e",
  productTitle: "Uzun isimli test yüzüğü",
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
  productUrl: "özel-yüzük",
  imageUrl: "https://res.cloudinary.com/demo/image/upload/order.jpg",
  imageAlt: null,
};

describe("order item media", () => {
  // Burada snapshot görseli ve slug bulunduğunda doğru link ile ürün başlığının alt fallback'ini kullandığımı doğruluyorum.
  it("renders the snapshot image with encoded product link and accessible alt fallback", () => {
    const html = renderToStaticMarkup(<ul><OrderItemMedia item={baseItem} /></ul>);
    expect(html).toContain('href="/products/%C3%B6zel-y%C3%BCz%C3%BCk"');
    expect(html).toContain('alt="Uzun isimli test yüzüğü"');
    expect(html).toContain("order.jpg");
    expect(html).toContain("Renk: Pudra");
    expect(html).not.toContain("SKU-1");
  });

  // Burada eski siparişte medya ve slug yoksa ürün linki üretmeden sabit fallback geometrisini koruduğumu doğruluyorum.
  it("renders a stable fallback without a product link", () => {
    const html = renderToStaticMarkup(<ul><OrderItemMedia item={{ ...baseItem, productUrl: null, imageUrl: null, imageAlt: null }} /></ul>);
    expect(html).not.toContain("/products/");
    expect(html).toContain("Görsel yok");
    expect(html).toContain("aspect-[4/5]");
  });

  // Burada migration öncesi veya varyantsız siparişte teknik SKU ve boş varyant ayıracı göstermediğimi doğruluyorum.
  it("hides variant metadata when the order snapshot fields are null", () => {
    const html = renderToStaticMarkup(<ul><OrderItemMedia item={{ ...baseItem, variantName: null, variantValue: null, variantSku: "Default" }} /></ul>);
    expect(html).not.toContain("Default");
    expect(html).not.toContain("Varsayılan");
    expect(html).not.toContain("undefined");
    expect(html).not.toContain("Renk:");
  });
});
