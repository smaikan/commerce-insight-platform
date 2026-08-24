import { renderToStaticMarkup } from "react-dom/server";
import { describe, expect, it, vi } from "vitest";

import { SiteHeader } from "./site-header";
import { HeaderSessionProvider } from "@/modules/auth/components/header-session";

// Burada header içindeki pathname ve arama navigasyon bağımlılıklarını statik sunucu render testinde güvenli biçimde sabitliyorum.
vi.mock("next/navigation", () => ({ usePathname: () => "/", useRouter: () => ({ push: vi.fn() }) }));
vi.mock("@/modules/catalog/navigation", () => ({
  getStorefrontNavigation: async () => [
    {
      id: "categories",
      label: "Kategoriler",
      items: [{ id: "type-1", label: "Yüzük", href: "/category/yuzuk", productCount: 8 }],
    },
    {
      id: "collections",
      label: "Koleksiyonlar",
      href: "/collections",
      items: [{ id: "collection-1", label: "Takı", href: "/collection/taki", productCount: 5 }],
    },
    {
      id: "brands",
      label: "Markalar",
      items: [{ id: "brand-1", label: "ELEVEN", href: "/brand/eleven", productCount: 6 }],
    },
  ],
}));
vi.mock("@/modules/auth/actions", () => ({ logoutAction: vi.fn() }));
vi.mock("@/modules/store-settings/api", () => ({
  getPublicStoreSettings: async () => ({
    displayName: "ELEVEN",
    logoUrl: "https://res.cloudinary.com/demo/image/upload/store-settings/logo/header.webp",
  }),
}));

describe("site header auth navigation", () => {
  // Burada ilk HTML'de yanlış guest/auth durumu göstermeden sabit hesap alanı ayrıldığını ve para biriminin kaldırıldığını doğruluyorum.
  it("reserves a stable account area until session state is known", async () => {
    const html = renderToStaticMarkup(<HeaderSessionProvider>{await SiteHeader()}</HeaderSessionProvider>);

    expect(html).not.toContain('href="/login"');
    expect(html).not.toContain('href="/register"');
    expect(html).toContain("h-10 w-44");
    expect(html).toContain("Hesap durumu yükleniyor");
    expect(html).toContain('aria-label="Favorilerim"');
    expect(html).toContain('data-scroll-state="visible"');
    expect(html).not.toContain("TR · TRY");
  });

  // Burada API'den gelen açık zemin logosunun sabit ve responsive boyutlarla ana sayfa bağlantısında kullanıldığını doğruluyorum.
  it("renders the configured light logo in the header", async () => {
    const html = renderToStaticMarkup(<HeaderSessionProvider>{await SiteHeader()}</HeaderSessionProvider>);

    expect(html).toContain('aria-label="ELEVEN ana sayfa"');
    expect(html).toContain('alt="ELEVEN"');
    expect(html).toContain("store-settings%2Flogo%2Fheader.webp");
    expect(html).toContain('sizes="(min-width: 640px) 64px, 44px"');
    expect(html).toContain("size-11");
    expect(html).toContain("left-1/2");
    expect(html).toContain("-translate-x-1/2");
  });

  // Burada masaüstü menüsünün sola yerleştiğini ve marka grubunun hem desktop hem mobil header ağacından çıkarıldığını doğruluyorum.
  it("aligns navigation left and removes the brands group from the header", async () => {
    const html = renderToStaticMarkup(<HeaderSessionProvider>{await SiteHeader()}</HeaderSessionProvider>);

    expect(html).toContain("lg:w-[calc(50%_-_3rem)]");
    expect(html).toContain("justify-start");
    expect(html).toContain("lg:px-4");
    expect(html).toContain("xl:px-6");
    expect(html).toContain("-mr-2");
    expect(html).toContain("sm:mr-0");
    expect(html).toContain("Kategoriler");
    expect(html).toContain("Koleksiyonlar");
    expect(html).not.toContain("Markalar");
    expect(html).not.toContain('/brand/eleven');
  });

  // Burada mobil header aksiyonlarının daha dar ölçülerle logodan uzaklaştığını ve masaüstü ölçülerinin korunduğunu doğruluyorum.
  it("uses compact mobile action sizing and spacing", async () => {
    const html = renderToStaticMarkup(<HeaderSessionProvider>{await SiteHeader()}</HeaderSessionProvider>);

    expect((html.match(/size-9/g) || []).length).toBeGreaterThanOrEqual(3);
    expect((html.match(/sm:size-11/g) || [])).toHaveLength(3);
    expect((html.match(/p-0!/g) || [])).toHaveLength(3);
    expect(html).toContain("ml-0.5 border-l border-line pl-0.5 sm:ml-2 sm:pl-1.5");
    expect(html).toContain("size-5 sm:size-6");
    expect(html).toContain("size-4.5 sm:size-5");
  });

  // Burada üst şeridin duyuruları ve sağda sabit İletişim linkini taşıdığını doğruluyorum.
  it("renders the store announcement strip with contact link", async () => {
    const html = renderToStaticMarkup(<HeaderSessionProvider>{await SiteHeader()}</HeaderSessionProvider>);

    expect(html).toContain('aria-label="Mağaza duyuruları"');
    expect(html).toContain("Ücretsiz Kargo");
    expect(html).toContain("Vade Farksız 3 Taksit");
    expect(html).toContain("Aynı Gün Kargo");
    expect(html).toContain("Kolay İade");
    expect(html).toContain("7/24 Canlı Destek");
    expect(html).toContain('aria-hidden="true"');
    expect(html).toContain('href="/contact"');
    expect(html).toContain("İletişim");
    expect(html).not.toContain("Duyuru hareketini duraklat veya başlat");
    expect(html).not.toContain("•");
  });
});
