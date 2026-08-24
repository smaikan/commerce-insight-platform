import { describe, expect, it } from "vitest";

import {
  BANNER_SECTION_CONFIGS,
  BANNER_SECTION_KEYS,
  isBannerSectionKey,
} from "./section-config";

describe("banner bölüm endpoint sözleşmesi", () => {
  it("yedi public endpointi doğru sırayla eşler", () => {
    expect(BANNER_SECTION_KEYS.map((key) => BANNER_SECTION_CONFIGS[key].publicPath)).toEqual([
      "/api/main-banners",
      "/api/main-banner-mobile",
      "/api/alt-banner-1",
      "/api/alt-banner-2",
      "/api/alt-banner-3",
      "/api/alt-banner-4",
      "/api/alt-banner-5",
    ]);
  });

  it("domain ana banner anahtarını çoğul endpoint yoluna bağlar", () => {
    expect(BANNER_SECTION_CONFIGS["main-banner"].publicPath).toBe("/api/main-banners");
    expect(BANNER_SECTION_CONFIGS["main-banner-mobile"].publicPath).toBe("/api/main-banner-mobile");
  });

  it("yalnız tanımlı kendi anahtarlarını kabul eder", () => {
    expect(isBannerSectionKey("main-banner")).toBe(true);
    expect(isBannerSectionKey("main-banner-mobile")).toBe(true);
    expect(isBannerSectionKey("toString")).toBe(false);
    expect(isBannerSectionKey("storefront-banners")).toBe(false);
  });
});
