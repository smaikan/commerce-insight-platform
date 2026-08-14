import { renderToStaticMarkup } from "react-dom/server";
import { describe, expect, it, vi } from "vitest";

import { SiteHeader } from "./site-header";
import { HeaderSessionProvider } from "@/modules/auth/components/header-session";

// Burada header içindeki pathname ve arama navigasyon bağımlılıklarını statik sunucu render testinde güvenli biçimde sabitliyorum.
vi.mock("next/navigation", () => ({ usePathname: () => "/", useRouter: () => ({ push: vi.fn() }) }));
vi.mock("@/modules/catalog/navigation", () => ({ getStorefrontNavigation: async () => [] }));
vi.mock("@/modules/auth/actions", () => ({ logoutAction: vi.fn() }));

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

  // Burada üst şeridin kullanıcı tarafından onaylanan hizmet taahhütlerini erişilebilir tek bir listede taşıdığını doğruluyorum.
  it("renders the store announcement strip", async () => {
    const html = renderToStaticMarkup(<HeaderSessionProvider>{await SiteHeader()}</HeaderSessionProvider>);

    expect(html).toContain('aria-label="Mağaza duyuruları"');
    expect(html).not.toContain("SERANTIS · Online mağaza");
    expect(html).toContain("Ücretsiz Kargo");
    expect(html).toContain("Vade Farksız 3 Taksit");
    expect(html).toContain("Aynı Gün Kargo");
    expect(html).toContain("Kolay İade");
    expect(html).toContain("7/24 Canlı Destek");
    expect(html).toContain('aria-hidden="true"');
    expect(html).toContain("Duyuru hareketini duraklat veya başlat");
    expect(html).not.toContain("•");
  });
});
