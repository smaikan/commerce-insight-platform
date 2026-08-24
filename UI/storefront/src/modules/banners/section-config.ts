import type { BannerSectionConfig, BannerSectionKey } from "./types";

// Burada vitrindeki bölüm sırasını API sözleşmesindeki yedi bağımsız alanla sabitliyorum.
export const BANNER_SECTION_KEYS = [
  "main-banner",
  "main-banner-mobile",
  "alt-banner-1",
  "alt-banner-2",
  "alt-banner-3",
  "alt-banner-4",
  "alt-banner-5",
] as const satisfies readonly BannerSectionKey[];

// Burada endpoint, yönetim yolu ve Cloudinary klasör eşleşmesini tek bir sözleşme tablosunda tutuyorum.
export const BANNER_SECTION_CONFIGS: Record<BannerSectionKey, BannerSectionConfig> = {
  "main-banner": {
    key: "main-banner",
    label: "Ana banner (Masaüstü)",
    publicPath: "/api/main-banners",
    adminPath: "/api/main-banners/admin",
    folder: "banners/main-banner",
    isMain: true,
  },
  "main-banner-mobile": {
    key: "main-banner-mobile",
    label: "Ana banner (Mobil)",
    publicPath: "/api/main-banner-mobile",
    adminPath: "/api/main-banner-mobile/admin",
    folder: "banners/main-banner-mobile",
    isMain: false,
  },
  "alt-banner-1": {
    key: "alt-banner-1",
    label: "Alt banner 1",
    publicPath: "/api/alt-banner-1",
    adminPath: "/api/alt-banner-1/admin",
    folder: "banners/alt-banner-1",
    isMain: false,
  },
  "alt-banner-2": {
    key: "alt-banner-2",
    label: "Alt banner 2",
    publicPath: "/api/alt-banner-2",
    adminPath: "/api/alt-banner-2/admin",
    folder: "banners/alt-banner-2",
    isMain: false,
  },
  "alt-banner-3": {
    key: "alt-banner-3",
    label: "Alt banner 3",
    publicPath: "/api/alt-banner-3",
    adminPath: "/api/alt-banner-3/admin",
    folder: "banners/alt-banner-3",
    isMain: false,
  },
  "alt-banner-4": {
    key: "alt-banner-4",
    label: "Alt banner 4",
    publicPath: "/api/alt-banner-4",
    adminPath: "/api/alt-banner-4/admin",
    folder: "banners/alt-banner-4",
    isMain: false,
  },
  "alt-banner-5": {
    key: "alt-banner-5",
    label: "Alt banner 5",
    publicPath: "/api/alt-banner-5",
    adminPath: "/api/alt-banner-5/admin",
    folder: "banners/alt-banner-5",
    isMain: false,
  },
};

// Burada yalnız kendi sözleşme anahtarlarımızı kabul ederek prototype alanlarının bölüm gibi algılanmasını önlüyorum.
export function isBannerSectionKey(value: string): value is BannerSectionKey {
  return Object.hasOwn(BANNER_SECTION_CONFIGS, value);
}

// Burada çağıranların yol ve görünüm bilgisini aynı güvenli kaynaktan almasını sağlıyorum.
export function getBannerSectionConfig(key: BannerSectionKey): BannerSectionConfig {
  return BANNER_SECTION_CONFIGS[key];
}
