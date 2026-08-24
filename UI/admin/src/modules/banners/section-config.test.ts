import { describe, expect, it } from "vitest";
import { BANNER_SECTION_CONFIGS, BANNER_SECTION_KEYS, isBannerSectionKey } from "./section-config";

describe("banner section endpoints", () => {
  // Burada yedi bölümün public/admin yollarını ve ayrık Cloudinary klasörlerini backend sözleşmesiyle birebir doğruluyorum.
  it("maps all public, admin and update endpoints", () => {
    expect(BANNER_SECTION_KEYS.map((key) => ({
      key,
      publicPath: BANNER_SECTION_CONFIGS[key].publicPath,
      adminPath: BANNER_SECTION_CONFIGS[key].adminPath,
      folder: BANNER_SECTION_CONFIGS[key].folder,
    }))).toEqual([
      { key: "main-banner", publicPath: "/api/main-banners", adminPath: "/api/main-banners/admin", folder: "banners/main-banner" },
      { key: "main-banner-mobile", publicPath: "/api/main-banner-mobile", adminPath: "/api/main-banner-mobile/admin", folder: "banners/main-banner-mobile" },
      { key: "alt-banner-1", publicPath: "/api/alt-banner-1", adminPath: "/api/alt-banner-1/admin", folder: "banners/alt-banner-1" },
      { key: "alt-banner-2", publicPath: "/api/alt-banner-2", adminPath: "/api/alt-banner-2/admin", folder: "banners/alt-banner-2" },
      { key: "alt-banner-3", publicPath: "/api/alt-banner-3", adminPath: "/api/alt-banner-3/admin", folder: "banners/alt-banner-3" },
      { key: "alt-banner-4", publicPath: "/api/alt-banner-4", adminPath: "/api/alt-banner-4/admin", folder: "banners/alt-banner-4" },
      { key: "alt-banner-5", publicPath: "/api/alt-banner-5", adminPath: "/api/alt-banner-5/admin", folder: "banners/alt-banner-5" },
    ]);
    expect(BANNER_SECTION_CONFIGS["main-banner"].isMain).toBe(true);
    expect(BANNER_SECTION_KEYS.slice(1).every((key) => !BANNER_SECTION_CONFIGS[key].isMain)).toBe(true);
  });

  // Burada yalnız kendi bölüm anahtarlarımızın kabul edilip prototip anahtarlarının reddedildiğini doğruluyorum.
  it("recognizes only owned section keys", () => {
    expect(isBannerSectionKey("main-banner")).toBe(true);
    expect(isBannerSectionKey("main-banner-mobile")).toBe(true);
    expect(isBannerSectionKey("toString")).toBe(false);
    expect(isBannerSectionKey("unknown")).toBe(false);
  });
});
