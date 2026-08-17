import { renderToStaticMarkup } from "react-dom/server";
import { describe, expect, it, vi } from "vitest";

vi.mock("@/modules/cart/client/cart-api", () => ({
  cartErrorMessage: () => "Hata",
  isConflictProblem: () => false,
  loadCart: () => new Promise(() => undefined),
  mutateCart: () => new Promise(() => undefined),
  subscribeToCart: () => () => undefined,
}));

vi.mock("next/navigation", () => ({
  useRouter: () => ({ push: vi.fn() }),
}));

vi.mock("@/modules/checkout/actions", () => ({
  createMemberOrderAction: vi.fn(),
}));

import { CartIndicator } from "@/components/storefront/cart-indicator";
import { CheckoutForm } from "@/modules/checkout/components/checkout-form";
import type { Cart } from "@/modules/cart/types";

import { CartItemIdentity, CartView } from "./cart-view";

// Burada sepet bileşen testi için generated CartDto biçimindeki tek bir gerçekçi satırı kullanıyorum.
const cartItem: Cart["items"][number] = {
  id: "8d52d55c-1acd-4c54-a9a0-3354e9f0d263",
  productId: "P00001",
  productVariantId: "a71e05d8-d9ce-4351-88f2-1b52580ae39e",
  productTitle: "Pudra yüzük",
  mainImage: {
    id: "940f452c-7b31-4f35-b774-18ac4ae043b7",
    productId: "P00001",
    imageUrl: "https://res.cloudinary.com/example/image/upload/product.jpg",
    altText: "Pudra yüzük ana görseli",
    displayOrder: 0,
    isMain: true,
  },
  variantName: "Renk",
  variantValue: "Pudra",
  sku: "SKU-PUDRA",
  quantity: 1,
  unitPrice: 1200,
  currentUnitPrice: 1200,
  totalPrice: 1200,
  availableStock: 4,
  isAvailable: true,
  priceChanged: false,
  createdAt: "2026-08-13T08:00:00Z",
};

describe("cart hydration boundary", () => {
  // Burada sepet sayfasının SSR ve ilk client render için ortak, deterministik loading ağacını ürettiğini doğruluyorum.
  it("starts the cart page from its loading state", () => {
    const html = renderToStaticMarkup(<CartView currency="TRY" />);
    expect(html).toContain("Sepet yükleniyor");
    expect(html).not.toContain("Sepetiniz henüz boş");
  });

  // Burada checkout'un client snapshot zamanlamasından bağımsız olarak loading ağacından başladığını doğruluyorum.
  it("starts checkout from its loading state", () => {
    const html = renderToStaticMarkup(
      <CheckoutForm
        shippingMethods={[]}
        currency="TRY"
        turnstileSiteKey=""
        orderCreationEnabled={false}
        accountAddresses={null}
      />,
    );
    expect(html).toContain("Sipariş sayfası yükleniyor");
    expect(html).not.toContain("Teslimat bilgileri");
  });

  // Burada header sepet sayacının hydration öncesi kararlı sıfır etiketiyle başlayıp görünür rozet üretmediğini doğruluyorum.
  it("starts the cart indicator without a stale client-only badge", () => {
    const html = renderToStaticMarkup(<CartIndicator />);
    expect(html).toContain('aria-label="Sepet, 0 ürün"');
    expect(html).not.toContain("99+");
  });

  // Burada sepet satırının ad/değer çiftini birlikte gösterip SKU'yu varyant fallback'i olarak sunmadığını doğruluyorum.
  it("renders the selected cart variant without a SKU fallback", () => {
    const html = renderToStaticMarkup(<CartItemIdentity item={cartItem} />);
    expect(html).toContain("Renk: Pudra");
    expect(html).not.toContain("SKU-PUDRA");
  });

  // Burada CartItemDto içindeki authoritative ana görseli ve alt metni ek ürün isteği olmadan render ettiğimi doğruluyorum.
  it("renders the cart main image from the cart response", () => {
    const html = renderToStaticMarkup(<CartItemIdentity item={cartItem} />);
    expect(html).toContain("Pudra yüzük ana görseli");
    expect(html).toContain("product.jpg");
    expect(html).not.toContain("Görsel yok");
  });

  // Burada görselsiz ürünün kırık img yerine açıklayıcı ve deterministik fallback sunduğunu doğruluyorum.
  it("renders a fallback when the cart item has no main image", () => {
    const html = renderToStaticMarkup(<CartItemIdentity item={{ ...cartItem, mainImage: undefined }} />);
    expect(html).toContain("Görsel yok");
    expect(html).not.toContain("<img");
  });

  // Burada varyantsız üründe teknik varsayılan metin veya boş varyant satırı üretmediğimi doğruluyorum.
  it("hides technical variant data for a non-variant product", () => {
    const html = renderToStaticMarkup(<CartItemIdentity item={{ ...cartItem, variantName: null, variantValue: null, sku: "Default" }} />);
    expect(html).not.toContain("Default");
    expect(html).not.toContain("Varsayılan");
    expect(html).not.toContain("undefined");
  });
});
