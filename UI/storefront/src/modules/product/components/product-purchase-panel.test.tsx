import { renderToStaticMarkup } from "react-dom/server";
import { describe, expect, it } from "vitest";

import { ProductPurchasePanel, type PurchaseVariant } from "./product-purchase-panel";

const singleVariant: PurchaseVariant = {
  id: "00000000-0000-0000-0000-000000000001",
  name: "Standart",
  value: "Tek Seçenek",
  price: 199,
  stock: 10,
};

describe("product purchase panel", () => {
  // Burada varyantsız ürünün satış kaydını sepete ekleme için korurken seçim arayüzünü göstermediğini doğruluyorum.
  it("hides option controls for a product without variants", () => {
    const html = renderToStaticMarkup(
      <ProductPurchasePanel variants={[singleVariant]} currency="TRY" showVariantSelection={false} />,
    );

    expect(html).toContain("Sepete ekle");
    expect(html).not.toContain("Ürün seçeneği");
    expect(html).not.toContain('type="radio"');
  });

  // Burada tek satış kaydı olsa bile API tercihi varyantlıysa seçim kontrolünün görünür kaldığını doğruluyorum.
  it("shows option controls when the product explicitly has variants", () => {
    const html = renderToStaticMarkup(
      <ProductPurchasePanel variants={[singleVariant]} currency="TRY" showVariantSelection />,
    );

    expect(html).toContain("Ürün seçeneği");
    expect(html).toContain('type="radio"');
  });
});
