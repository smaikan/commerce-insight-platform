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
  logoUrl: null,
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
  // Burada dört müşteri/yasal sayfanın doğru İngilizce URL'lerle footer içinde erişilebilir olduğunu doğruluyorum.
  it("renders legal and customer information links", () => {
    const html = renderToStaticMarkup(<SiteFooterView settings={settings} categories={[]} />);

    expect(html).toContain('href="/distance-sales-agreement"');
    expect(html).toContain('href="/payment-and-delivery"');
    expect(html).toContain('href="/cancellation-and-refund"');
    expect(html).toContain('href="/privacy-policy"');
    expect(html).toContain("Mesafeli Satış Sözleşmesi");
    expect(html).toContain("KVKK ve Gizlilik Politikası");
  });

  // Burada API'nin yayınladığı iletişim, sosyal hesap ve kategori bilgilerinin footer'a güvenli hedeflerle taşındığını doğruluyorum.
  it("renders public store identity, contact and category data", () => {
    const html = renderToStaticMarkup(
      <SiteFooterView
        settings={settings}
        categories={[{ id: "rings", label: "Yüzük", href: "/category/yuzuk", productCount: 4 }]}
      />,
    );

    expect(html).toContain("SERANTIS");
    expect(html).toContain('href="mailto:destek@example.com"');
    expect(html).toContain('href="tel:+902120000000"');
    expect(html).toContain('href="https://wa.me/905550000000"');
    expect(html).toContain('href="/category/yuzuk"');
    expect(html).toContain('href="https://instagram.com/example"');
    expect(html).toContain("lg:pt-11");
    expect(html).toContain("lg:pb-8");
    expect(html).toContain("py-3.5");
    expect(html).not.toContain("lg:py-16");
  });
});
