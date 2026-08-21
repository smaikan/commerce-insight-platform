import { renderToStaticMarkup } from "react-dom/server";
import { describe, expect, it, vi } from "vitest";

// Burada Server Component footer görünümünü statik markup testinde izole edebilmek için yalnız server-only koruyucusunu etkisizleştiriyorum.
vi.mock("server-only", () => ({}));

import type { PublicStoreSettings } from "@/modules/store-settings/types";

import { SiteFooterView } from "./site-footer";

// Burada footer testi için public StoreSettings sözleşmesinin görselde tüketilen alanlarını gerçekçi bir fixture ile dolduruyorum.
const settings = {
  displayName: "SERANTIS",
  shortDescription: "Özenle seçilen ürünler.",
  logoUrl: "https://res.cloudinary.com/demo/image/upload/store-settings/logo/light.webp",
  darkLogoUrl: "https://res.cloudinary.com/demo/image/upload/store-settings/dark-logo/footer.webp",
  supportEmail: "destek@example.com",
  supportPhone: "+90 212 000 00 00",
  whatsappNumber: "+90 555 000 00 00",
  contactAddress: "İstanbul, Türkiye",
  workingHours: "Hafta içi 09.00–18.00",
  mapUrl: "https://maps.example.com/store",
  facebookUrl: null,
  instagramUrl: "https://instagram.com/example",
  tiktokUrl: null,
  youtubeUrl: null,
  xUrl: null,
  pinterestUrl: null,
} satisfies Partial<PublicStoreSettings>;

describe("site footer", () => {
  // Burada gerekli dört yasal sayfanın footer'da kaldığını, iki üyelik metninin ise kaldırıldığını doğruluyorum.
  it("renders only the compact footer legal links", () => {
    const html = renderToStaticMarkup(<SiteFooterView settings={settings} collections={[]} />);

    expect(html).toContain('href="/distance-sales-agreement"');
    expect(html).toContain('href="/payment-and-delivery"');
    expect(html).toContain('href="/cancellation-and-refund"');
    expect(html).toContain('href="/privacy-policy"');
    expect(html).toContain("Mesafeli Satış Sözleşmesi");
    expect(html).toContain("KVKK ve Gizlilik Politikası");
    expect(html).not.toContain('href="/membership-agreement"');
    expect(html).not.toContain('href="/membership-privacy-notice"');
  });

  // Burada API'nin yayınladığı iletişim, sosyal hesap ve koleksiyon bilgilerinin footer'a güvenli hedeflerle taşındığını doğruluyorum.
  it("renders public store identity, contact and collection data", () => {
    const html = renderToStaticMarkup(
      <SiteFooterView
        settings={settings}
        collections={[{ id: "daily", label: "Günlük", href: "/collection/gunluk", productCount: 4 }]}
      />,
    );

    expect(html).toContain("SERANTIS");
    expect(html).toContain('href="mailto:destek@example.com"');
    expect(html).toContain('href="tel:+902120000000"');
    expect(html).toContain('href="https://wa.me/905550000000"');
    expect(html).toContain('href="/collection/gunluk"');
    expect(html).toContain("Koleksiyonlar");
    expect(html).toContain('href="https://instagram.com/example"');
    expect(html).toContain("lg:pt-11");
    expect(html).toContain("lg:pb-8");
    expect(html).toContain("py-3.5");
    expect(html).not.toContain("lg:py-16");
  });

  // Burada footer'ın koyu zemin logosunu standart logoya tercih edip daha büyük responsive alanda gösterdiğini doğruluyorum.
  it("prefers the configured dark logo in the footer", () => {
    const html = renderToStaticMarkup(<SiteFooterView settings={settings} collections={[]} />);

    expect(html).toContain("store-settings%2Fdark-logo%2Ffooter.webp");
    expect(html).not.toContain("store-settings%2Flogo%2Flight.webp");
    expect(html).toContain('sizes="(min-width: 640px) 112px, 96px"');
    expect(html).toContain("size-24");
  });

  // Burada görünüm katmanının verilen koleksiyon bağlantılarını semantik koleksiyon navigasyonunda gösterdiğini doğruluyorum.
  it("renders collection navigation with the collections fallback", () => {
    const populatedHtml = renderToStaticMarkup(
      <SiteFooterView
        settings={settings}
        collections={[
          { id: "one", label: "Bir", href: "/collection/bir", productCount: 1 },
          { id: "two", label: "İki", href: "/collection/iki", productCount: 1 },
        ]}
      />,
    );
    const emptyHtml = renderToStaticMarkup(<SiteFooterView settings={settings} collections={[]} />);

    expect(populatedHtml).toContain('aria-label="Footer koleksiyonları"');
    expect(populatedHtml).toContain('href="/collection/bir"');
    expect(populatedHtml).toContain('href="/collection/iki"');
    expect(emptyHtml).toContain('href="/collections"');
    expect(emptyHtml).toContain("Tüm koleksiyonlar");
  });

  // Burada görünüm katmanının verilen kategori bağlantılarını semantik kategori navigasyonunda gösterdiğini doğruluyorum.
  it("renders category navigation with the categories fallback", () => {
    const populatedHtml = renderToStaticMarkup(
      <SiteFooterView
        settings={settings}
        collections={[]}
        categories={[
          { id: "cat1", label: "Kategori 1", href: "/category/kategori-1", productCount: 5 },
        ]}
      />,
    );
    const emptyHtml = renderToStaticMarkup(<SiteFooterView settings={settings} collections={[]} />);

    expect(populatedHtml).toContain('aria-label="Footer kategorileri"');
    expect(populatedHtml).toContain('href="/category/kategori-1"');
    expect(emptyHtml).toContain('href="/categories"');
    expect(emptyHtml).toContain("Tüm kategoriler");
  });

  // Burada footer'ın yoğun koleksiyon verisinde ilk altı bağlantıyla sınırlı kaldığını doğruluyorum.
  it("limits collection navigation to six items", () => {
    const collections = Array.from({ length: 7 }, (_, index) => ({
      id: `collection-${index + 1}`,
      label: `Koleksiyon ${index + 1}`,
      href: `/collection/koleksiyon-${index + 1}`,
      productCount: index + 1,
    }));
    const html = renderToStaticMarkup(<SiteFooterView settings={settings} collections={collections} />);

    expect(html).toContain("Koleksiyon 6");
    expect(html).not.toContain("Koleksiyon 7");
  });

  // Burada footer başlıklarının mobilde kapalı disclosure, masaüstünde ise sürekli görünür içerik olarak sunulduğunu doğruluyorum.
  it("renders accessible mobile footer disclosures", () => {
    const html = renderToStaticMarkup(<SiteFooterView settings={settings} collections={[]} />);

    expect(html.match(/aria-expanded="false"/g)).toHaveLength(4);
    expect(html).toContain('aria-controls="footer-contact-panel"');
    expect(html).toContain('aria-controls="footer-categories-panel"');
    expect(html).toContain('aria-controls="footer-collections-panel"');
    expect(html).toContain('aria-controls="footer-customer-panel"');
    expect(html).toContain("sm:hidden");
    expect(html).toContain("sm:block");
  });
});
