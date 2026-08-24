import { renderToStaticMarkup } from "react-dom/server";
import { describe, expect, it, vi } from "vitest";

import { PromoRibbon } from "./promo-ribbon";
import { LookbookBanner } from "./lookbook-banner";
import { CustomerReviewsSection } from "./customer-reviews-section";
import { NewsletterSection } from "./newsletter-section";
import { FeaturedProductsTabs } from "./featured-products-tabs";
import { HomeBentoCategories } from "./home-bento-categories";
import { HomeCuratedCollections } from "./home-curated-collections";
import { CraftsmanshipStrip } from "./craftsmanship-strip";
import { InstagramGallerySection } from "./instagram-gallery-section";
import type { PublishedProduct } from "@/modules/catalog/types";

vi.mock("next/navigation", () => ({ useRouter: () => ({ push: vi.fn(), refresh: vi.fn() }) }));
vi.mock("@/modules/auth/components/header-session", () => ({ useHeaderSession: () => "authenticated" }));

const sampleProduct: PublishedProduct = {
  id: "P00001",
  title: "Test Küpe",
  url: "test-kupe",
  summary: null,
  brandName: "ELEVEN",
  price: 599,
  compareAtPrice: 799,
  averageRating: 0,
  ratingCount: 0,
  mainImage: undefined,
  isAvailable: true,
  lowestAvailableStock: 10,
  isLowStock: false,
};

describe("Home page components", () => {
  it("renders PromoRibbon with 4 benefits", () => {
    const html = renderToStaticMarkup(<PromoRibbon />);
    expect(html).toContain("Ücretsiz Kargo");
    expect(html).toContain("Güvenli Alışveriş");
    expect(html).toContain("Kolay İade &amp; Değişim");
    expect(html).toContain("Hızlı Sevkiyat");
  });

  it("renders LookbookBanner with shoppable hotspot pin", () => {
    const html = renderToStaticMarkup(<LookbookBanner />);
    // Mobil lookbook görselinin portre oranını koruduğunu ve ürün etiketini kart içinde ortaladığını doğruluyorum.
    expect(html).toContain("aspect-[4/5] w-full");
    expect(html).toContain("left-1/2");
    expect(html).toContain("LOOKBOOK");
    expect(html).toContain("Koleksiyonu Keşfet");
    expect(html).toContain("Sculptural Choker");
    expect(html).toContain("1.299 TL");
  });

  it("renders CustomerReviewsSection with satisfaction rating and reviews", () => {
    const html = renderToStaticMarkup(<CustomerReviewsSection />);
    expect(html).toContain("Müşterilerimizin Deneyimleri");
    expect(html).toContain("4.9 / 5.0 Memnuniyet");
    expect(html).toContain("Selin D.");
    expect(html).toContain("Doğrulanmış Alıcı");
  });

  it("renders NewsletterSection with invitation form", () => {
    const html = renderToStaticMarkup(<NewsletterSection />);
    expect(html).toContain("ELEVEN AYRICALIKLAR KULÜBÜ");
    expect(html).toContain("İlk Alışverişinizde %15 İndirim");
    expect(html).toContain("Katılın");
  });

  it("renders FeaturedProductsTabs with products", () => {
    const html = renderToStaticMarkup(
      <FeaturedProductsTabs
        bestSellers={[sampleProduct]}
        newArrivals={[sampleProduct]}
      />
    );
    expect(html).toContain("ÖNE ÇIKAN MODELLER");
    expect(html).toContain("En Çok Satanlar");
    expect(html).toContain("Yeni Gelenler");
    expect(html).toContain("Test Küpe");
  });

  it("renders HomeBentoCategories with asymmetric cards", () => {
    const html = renderToStaticMarkup(
      <HomeBentoCategories
        categories={[
          { id: "1", name: "Küpe", href: "/category/kupe", imageAlt: "Küpe", imageUrl: "https://example.com/kupe.webp", productCount: 15 },
          { id: "2", name: "Bileklik", href: "/category/bileklik", imageAlt: "Bileklik", imageUrl: null, productCount: 8 },
        ]}
      />
    );
    expect(html).toContain("Tarzınızı Tamamlayan Kategoriler");
    expect(html).toContain("Küpe");
    expect(html).toContain("15 Farklı Model");
    expect(html).toContain("Bileklik");
  });

  it("renders HomeCuratedCollections with editorial cards and watermark numerals", () => {
    const html = renderToStaticMarkup(
      <HomeCuratedCollections
        collections={[
          { id: "1", name: "City Edit", url: "city-edit", href: "/collection/city-edit", imageAlt: "City Edit", imageUrl: "https://example.com/city.webp", productCount: 14, isFeatured: true, displayOrder: 1 },
        ]}
      />
    );
    expect(html).toContain("İlham Veren Tematik Koleksiyonlar");
    expect(html).toContain("City Edit");
    expect(html).toContain("14 Özel Parça");
    expect(html).toContain("01");
  });

  it("renders CraftsmanshipStrip with 3 quality pillars", () => {
    const html = renderToStaticMarkup(<CraftsmanshipStrip />);
    expect(html).toContain("ELEVEN KALİTE STANDARTLARI");
    expect(html).toContain("18K Altın &amp; Rodyum Kaplama");
    expect(html).toContain("Antialerjik &amp; Cilt Dostu");
    expect(html).toContain("Özel Lüks Hediye Paketi");
  });

  it("renders InstagramGallerySection with lifestyle posts", () => {
    const html = renderToStaticMarkup(<InstagramGallerySection />);
    expect(html).toContain("#ElevenWomen");
    expect(html).toContain("@elevenaccessory");
  });
});
