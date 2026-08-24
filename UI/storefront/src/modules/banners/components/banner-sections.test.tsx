import { renderToStaticMarkup } from "react-dom/server";
import { describe, expect, it } from "vitest";
import { AlternateBannerSection, BannerMedia, MainBannerSection } from "./banner-sections";
import type { BannerSection, BannerSectionItem } from "../types";

// Burada testlerde yalnız render davranışını doğrulamak için belgelenmiş banner alanlarından kayıt üretiyorum.
function item(overrides: Partial<BannerSectionItem> = {}): BannerSectionItem {
  return {
    id: "00000000-0000-0000-0000-000000000001",
    name: "Yaz seçkisi",
    key: "summer",
    mediaUrl: "https://cdn.example.com/banner.webp",
    mediaType: 1,
    targetUrl: null,
    altText: "Yaz koleksiyonu",
    displayOrder: 1,
    isActive: true,
    isMain: false,
    ...overrides,
  };
}

describe("storefront banner sections", () => {
  // Burada boş alt bölümün gereksiz görünür veya semantik container üretmediğini doğruluyorum.
  it("renders nothing for an empty section", () => {
    const section: BannerSection = { name: "Alt Banner 1", key: "alt-banner-1", items: [] };
    expect(renderToStaticMarkup(<AlternateBannerSection section={section} />)).toBe("");
  });

  // Burada isMain kaydının API dizisindeki konumundan bağımsız olarak ilk ana medya olduğunu doğruluyorum.
  it("renders the selected main item before other active items", () => {
    const section: BannerSection = {
      name: "Main Banner",
      key: "main-banner",
      items: [
        item({ id: "secondary", mediaUrl: "https://cdn.example.com/secondary.webp", displayOrder: 0 }),
        item({ id: "primary", mediaUrl: "https://cdn.example.com/primary.webp", displayOrder: 4, isMain: true }),
      ],
    };
    const html = renderToStaticMarkup(<MainBannerSection section={section} />);
    expect(html.indexOf("primary.webp")).toBeLessThan(html.indexOf("secondary.webp"));
    expect(html).toContain('class="w-full"');
    expect(html).toContain('sizes=');
    expect(html).toContain('loading="eager"');
    expect(html).toContain('fetchPriority="high"');
  });

  // Burada masaüstü ve mobil banner'lar farklı olduğunda picture etiketiyle responsive art direction üretildiğini doğruluyorum.
  it("renders responsive picture tag when desktop and mobile banners differ", () => {
    const desktopSection: BannerSection = {
      name: "Desktop Main",
      key: "main-banner",
      items: [item({ id: "desktop-1", mediaUrl: "https://cdn.example.com/desktop.webp", isMain: true })],
    };
    const mobileSection: BannerSection = {
      name: "Mobile Main",
      key: "main-banner-mobile",
      items: [item({ id: "mobile-1", mediaUrl: "https://cdn.example.com/mobile.webp", isMain: true })],
    };
    const html = renderToStaticMarkup(
      <MainBannerSection desktopSection={desktopSection} mobileSection={mobileSection} />,
    );
    expect(html).toContain("<picture");
    expect(html).toContain('media="(min-width: 768px)"');
    expect(html).toContain("desktop.webp");
    expect(html).toContain("mobile.webp");
  });

  // Burada video medyasının sesli otomatik oynatma olmadan kontrollü ve mobil uyumlu oluşturulduğunu doğruluyorum.
  it("renders controlled muted video without autoplay", () => {
    const html = renderToStaticMarkup(<BannerMedia item={item({ mediaType: 2, mediaUrl: "https://cdn.example.com/banner.mp4" })} variant="alternate" />);
    expect(html).toContain("<video");
    expect(html).toContain("controls");
    expect(html).toContain("muted");
    expect(html).toContain("playsInline");
    expect(html).not.toContain("autoplay");
  });

  // Burada güvenli hedefin wrapper linke dönüştüğünü, javascript hedefinin ise bağlantı üretmediğini doğruluyorum.
  it("wraps only safe image targets", () => {
    const safe = renderToStaticMarkup(<BannerMedia item={item({ targetUrl: "/collections/yaz" })} variant="alternate" />);
    const unsafe = renderToStaticMarkup(<BannerMedia item={item({ targetUrl: "javascript:alert(1)" })} variant="alternate" />);
    expect(safe).toContain('href="/collections/yaz"');
    expect(unsafe).not.toContain("href=");
  });
});
