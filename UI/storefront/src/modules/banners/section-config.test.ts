import { describe, expect, it } from "vitest";

import {
  BANNER_SECTION_CONFIGS,
  BANNER_SECTION_KEYS,
  isBannerSectionKey,
} from "./section-config";

describe("banner bölüm endpoint sözleşmesi", () => {
  it("altı public endpointi doğru sırayla eşler", () => {
    expect(BANNER_SECTION_KEYS.map((key) => BANNER_SECTION_CONFIGS[key].publicPath)).toEqual([
      "/api/main-banners",
      "/api/alt-banner-1",
      "/api/alt-banner-2",
      "/api/alt-banner-3",
      "/api/alt-banner-4",
      "/api/alt-banner-5",
    ]);
  });

  it("domain ana banner anahtarını çoğul endpoint yoluna bağlar", () => {
    expect(BANNER_SECTION_CONFIGS["main-banner"].publicPath).toBe("/api/main-banners");
  });

  it("yalnız tanımlı kendi anahtarlarını kabul eder", () => {
    expect(isBannerSectionKey("main-banner")).toBe(true);
    expect(isBannerSectionKey("toString")).toBe(false);
    expect(isBannerSectionKey("storefront-banners")).toBe(false);
  });
});
