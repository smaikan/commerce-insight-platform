import { renderToStaticMarkup } from "react-dom/server";
import { describe, expect, it, vi } from "vitest";

import type { SearchProduct } from "@/modules/search/types";

import { SearchInspiration, SearchOverlay, SearchResults } from "./search-overlay";

vi.mock("next/navigation", () => ({ useRouter: () => ({ push: vi.fn() }) }));

// Burada gerçek API verisi üretmeden yalnız test render'ında uzun ad, eksik görsel/fiyat ve stok dışı durumlarını kapsıyorum.
const products: SearchProduct[] = Array.from({ length: 11 }, (_, index) => ({
  id: `P${index + 1}`,
  title: index === 0 ? "Uzun İsimli Mavi Taşlı Tasarım Kolye" : `Arama Ürünü ${index + 1}`,
  url: index === 0 ? "backend-owned-url" : `api-url-${index + 1}`,
  brandName: index === 0 ? "Marka" : null,
  price: index === 1 ? null : 2_499.9 + index,
  compareAtPrice: index === 0 ? 2_799.9 : null,
  imageUrl: index === 0 ? "https://res.cloudinary.com/demo/search.jpg" : null,
  imageAlt: index === 0 ? "Mavi taşlı kolye" : null,
  isAvailable: index !== 2,
}));

describe("search overlay design", () => {
  // Burada navbar tetikleyicisinin erişilebilir dialog ilişkisini ve arama alanının açıklamasını taşıdığını doğruluyorum.
  it("renders an accessible full-screen search shell", () => {
    const html = renderToStaticMarkup(<SearchOverlay />);

    expect(html).toContain('aria-label="Ürün ara"');
    expect(html).toContain('aria-haspopup="dialog"');
    expect(html).toContain('aria-labelledby="search-dialog-title"');
    expect(html).toContain('placeholder="Ürün adı, marka veya kategori ara"');
    expect(html).toContain("En az iki karakter yazın");
    expect(html).toContain('aria-label="Aramayı kapat"');
  });

  // Burada API sonucunun en fazla on kartla, backend URL'siyle, doğrudan görselle ve tüm sonuçlar bağlantısıyla sunulduğunu doğruluyorum.
  it("renders a capped two-row product result design", () => {
    const html = renderToStaticMarkup(<SearchResults query="mavi kolye" products={products} hasMore />);

    expect((html.match(/<article/g) || [])).toHaveLength(10);
    expect(html).toContain('href="/products/backend-owned-url"');
    expect(html).not.toContain("uzun-isimli-mavi");
    expect(html).toContain('alt="Mavi taşlı kolye"');
    expect(html).toContain("Ürün görseli bulunmuyor");
    expect(html).toContain("Şu an mevcut değil");
    expect(html).toContain('href="/products?q=mavi%20kolye"');
    expect(html).toContain("grid-rows-2");
  });

  // Burada tüm sonuç bağlantısının hasMore false olsa da bulunan ürünleri tam katalogda açtığını doğruluyorum.
  it("keeps the all-results link for a non-empty final suggestion page", () => {
    const html = renderToStaticMarkup(<SearchResults query="inci" products={products.slice(0, 2)} hasMore={false} />);

    expect(html).toContain("Tümünü gör");
    expect(html).toContain('href="/products?q=inci"');
  });

  // Burada modal ilk açılışının istenen başlık ve tek sıralı en fazla beş ilham kartıyla sunulduğunu doğruluyorum.
  it("renders a single-row inspiration showcase", () => {
    const html = renderToStaticMarkup(<SearchInspiration products={products} />);

    expect(html).toContain("Biraz ilhama mı ihtiyacınız var?");
    expect((html.match(/<article/g) || [])).toHaveLength(5);
    expect(html).toContain("grid-flow-col");
  });

  // Burada boş arama sonucunda sahte kart üretmeden açıklayıcı boş durum gösterildiğini doğruluyorum.
  it("renders an honest empty result state", () => {
    const html = renderToStaticMarkup(<SearchResults query="bulunmayan" products={[]} hasMore={false} />);

    expect(html).toContain("Eşleşen ürün bulunamadı");
    expect(html).not.toContain("<article");
  });
});
