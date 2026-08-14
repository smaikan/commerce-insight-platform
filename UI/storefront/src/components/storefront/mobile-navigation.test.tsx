import { renderToStaticMarkup } from "react-dom/server";
import { describe, expect, it, vi } from "vitest";

import { MobileNavigation } from "./mobile-navigation";

vi.mock("@/modules/auth/actions", () => ({ logoutAction: vi.fn() }));

const groups = [
  {
    id: "categories" as const,
    label: "Kategoriler",
    items: [{ id: "type-1", label: "Yüzük", href: "/category/yuzuk", productCount: 8 }],
  },
  {
    id: "collections" as const,
    label: "Koleksiyonlar",
    href: "/collections",
    items: [{ id: "collection-1", label: "Takı", href: "/collection/taki", productCount: 5 }],
  },
  {
    id: "brands" as const,
    label: "Markalar",
    items: [{ id: "brand-1", label: "SERANTIS", href: "/brand/serantis", productCount: 6 }],
  },
];

describe("mobile navigation", () => {
  // Burada kapalı hamburger menüsünün erişilebilir adını, durumunu ve kontrol ettiği panel bağlantısını ilk HTML'de doğruluyorum.
  it("renders an accessible collapsed trigger", () => {
    const html = renderToStaticMarkup(<MobileNavigation siteName="SERANTIS" groups={groups} />);

    expect(html).toContain('aria-label="Menüyü aç"');
    expect(html).toContain('aria-expanded="false"');
    expect(html).toContain('aria-controls="mobile-navigation-panel"');
    expect(html).toContain('id="mobile-navigation-panel"');
    expect(html).toContain("<dialog");
    expect(html).toContain('aria-label="Navigasyon menüsü"');
  });

  // Burada mobil çekmecenin bağımsız akordeon bilgi mimarisini ve sunucudan gelen gerçek hedefleri koruduğunu doğruluyorum.
  it("renders catalog groups as compact accordions", () => {
    const html = renderToStaticMarkup(<MobileNavigation siteName="SERANTIS" groups={groups} />);

    expect(html).not.toContain("<details");
    expect(html).toContain("Kategoriler");
    expect(html).toContain("Koleksiyonlar");
    expect(html).toContain("Markalar");
    expect(html).toContain('href="/category/yuzuk"');
    expect(html).toContain('href="/collection/taki"');
    expect(html).toContain('href="/collections"');
    expect(html).toContain('aria-label="Koleksiyonlar alt menüsünü aç"');
    expect(html).toContain('href="/brand/serantis"');
    expect(html).toContain('href="/login"');
    expect(html).toContain('href="/register"');
    expect(html).toContain("Giriş yap");
    expect(html).toContain("Hesap oluştur");
  });
});
