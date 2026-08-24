import { renderToStaticMarkup } from "react-dom/server";
import { describe, expect, it, vi } from "vitest";

import type { FavoriteProduct, FavoriteProductPage } from "@/modules/favorites/types";

import { FavoritesView } from "./favorites-view";

vi.mock("next/navigation", () => ({ useRouter: () => ({ push: vi.fn(), refresh: vi.fn() }) }));
vi.mock("@/modules/auth/components/header-session", () => ({ useHeaderSession: () => "authenticated" }));

// Burada favori sayfasını generated ProductDto alanlarıyla ve API'nin eksik medya/fiyat durumuyla sınayan ürün oluşturuyorum.
function favoriteProduct(overrides: Partial<FavoriteProduct> = {}): FavoriteProduct {
  return {
    id: "P00001",
    title: "Uzun İsimli Favori Kolye",
    mainSku: "SKU-001",
    description: null,
    url: "backend-owned-favorite-url",
    typeId: null,
    typeName: null,
    brandId: null,
    brandName: "SERANTIS",
    taxRateId: null,
    taxRateName: null,
    taxRatePercentage: null,
    status: 1,
    isActive: true,
    isFeatured: false,
    hasVariants: false,
    displayOrder: 0,
    seoTitle: null,
    seoDescription: null,
    clickCount: 0,
    totalAddToCartCount: 0,
    totalPurchaseCount: 0,
    favoriteCount: 1,
    popularityScore: 0,
    averageRating: 0,
    ratingCount: 0,
    reviewCount: 0,
    variants: [],
    tags: [],
    collections: [],
    images: [],
    summary: null,
    mainImage: undefined,
    ...overrides,
  };
}

// Burada favori ürünleri ortak sayfalama sözleşmesine yerleştiriyorum.
function favoritePage(items: FavoriteProduct[]): FavoriteProductPage {
  return {
    items,
    pageNumber: 1,
    pageSize: 20,
    totalCount: items.length,
    totalPages: 1,
    hasPreviousPage: false,
    hasNextPage: false,
  };
}

describe("favorites view", () => {
  // Burada backend URL'si, null görsel/fiyat placeholder'ı ve favoriden çıkarma kontrolünün birlikte sunulduğunu doğruluyorum.
  it("renders favorite products without inventing media or price", () => {
    const html = renderToStaticMarkup(<FavoritesView products={favoritePage([favoriteProduct()])} />);

    expect(html).toContain('href="/products/backend-owned-favorite-url"');
    expect(html).toContain("Ürün görseli bulunmuyor");
    expect(html).toContain("Fiyat bilgisi mevcut değil");
    expect(html).toContain("1</span> kayıtlı ürün");
    expect(html).toContain("ürününü favorilerden çıkar");
  });

  // Burada kart fiyatı ve fallback görselinin ek detay isteği olmadan ProductDto varyant ve görsel dizilerinden üretildiğini doğruluyorum.
  it("uses variants and the first usable image from the favorite ProductDto", () => {
    const html = renderToStaticMarkup(<FavoritesView products={favoritePage([favoriteProduct({
      variants: [
        { id: "00000000-0000-0000-0000-000000000001", productId: "P00001", name: "Boyut", value: "Standart", sku: "SKU-001", price: 1250, netPrice: 1041.67, stock: 4, addToCartCount: 0, purchaseCount: 0, isActive: true, concurrencyToken: "variant-token" },
      ],
      images: [
        { id: "00000000-0000-0000-0000-000000000002", productId: "P00001", imageUrl: "https://cdn.example.test/favorite.jpg", altText: "Favori ürün", displayOrder: 0, isMain: false },
      ],
      mainImage: undefined,
    })])} />);

    expect(html).toContain("https%3A%2F%2Fcdn.example.test%2Ffavorite.jpg");
    expect(html).toContain("1.250,00");
    expect(html).not.toContain("Ürün görseli bulunmuyor");
  });

  // Burada satıştan kaldırılan favoriye kırık ürün linki vermeden durumunu açıkça gösterdiğimi doğruluyorum.
  it("keeps unavailable favorites visible without a product link", () => {
    const html = renderToStaticMarkup(<FavoritesView products={favoritePage([
      favoriteProduct({ id: "P00002", status: 3, isActive: false, url: "archived-product" }),
    ])} />);

    expect(html).toContain("Artık satışta değil");
    expect(html).not.toContain('href="/products/archived-product"');
  });

  // Burada boş favori hesabında sahte ürün üretmeden gerçek katalog dönüşünü sunduğumu doğruluyorum.
  it("renders an actionable empty state", () => {
    const html = renderToStaticMarkup(<FavoritesView products={favoritePage([])} />);

    expect(html).toContain("Henüz favori ürününüz yok");
    expect(html).toContain('href="/products"');
    expect(html).not.toContain("<article");
  });

  // Burada API bayraklarıyla kurulan sayfalama bağlantılarında page ve pageSize değerlerinin birlikte korunduğunu doğruluyorum.
  it("preserves page size in favorite pagination links", () => {
    const products = { ...favoritePage([favoriteProduct()]), pageNumber: 2, pageSize: 12, totalCount: 36, totalPages: 3, hasPreviousPage: true, hasNextPage: true };
    const html = renderToStaticMarkup(<FavoritesView products={products} />);

    expect(html).toContain('href="/account/favorites?page=1&amp;pageSize=12"');
    expect(html).toContain('href="/account/favorites?page=3&amp;pageSize=12"');
  });
});
