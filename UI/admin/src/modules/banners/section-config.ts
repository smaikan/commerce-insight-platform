// Burada altı bağımsız banner bölümünün sabit anahtarlarını backend bölüm anahtarlarıyla birebir tutuyorum.
export type BannerSectionKey =
  | "main-banner"
  | "alt-banner-1"
  | "alt-banner-2"
  | "alt-banner-3"
  | "alt-banner-4"
  | "alt-banner-5";

export type BannerSectionConfig = {
  key: BannerSectionKey;
  label: string;
  publicPath: string;
  adminPath: string;
  folder: string;
  isMain: boolean;
};

// Burada bölüm sırasını yönetim ve storefront kompozisyonlarının birlikte kullanacağı kararlı bir tuple olarak tutuyorum.
export const BANNER_SECTION_KEYS = [
  "main-banner",
  "alt-banner-1",
  "alt-banner-2",
  "alt-banner-3",
  "alt-banner-4",
  "alt-banner-5",
] as const satisfies readonly BannerSectionKey[];

// Burada public GET/PUT, admin GET ve Cloudinary klasörlerini anahtarla erişilen tek bölüm haritasında topluyorum.
export const BANNER_SECTION_CONFIGS: Record<BannerSectionKey, BannerSectionConfig> = {
  "main-banner": { key: "main-banner", label: "Main Banner", publicPath: "/api/main-banners", adminPath: "/api/main-banners/admin", folder: "banners/main-banner", isMain: true },
  "alt-banner-1": { key: "alt-banner-1", label: "Alt Banner 1", publicPath: "/api/alt-banner-1", adminPath: "/api/alt-banner-1/admin", folder: "banners/alt-banner-1", isMain: false },
  "alt-banner-2": { key: "alt-banner-2", label: "Alt Banner 2", publicPath: "/api/alt-banner-2", adminPath: "/api/alt-banner-2/admin", folder: "banners/alt-banner-2", isMain: false },
  "alt-banner-3": { key: "alt-banner-3", label: "Alt Banner 3", publicPath: "/api/alt-banner-3", adminPath: "/api/alt-banner-3/admin", folder: "banners/alt-banner-3", isMain: false },
  "alt-banner-4": { key: "alt-banner-4", label: "Alt Banner 4", publicPath: "/api/alt-banner-4", adminPath: "/api/alt-banner-4/admin", folder: "banners/alt-banner-4", isMain: false },
  "alt-banner-5": { key: "alt-banner-5", label: "Alt Banner 5", publicPath: "/api/alt-banner-5", adminPath: "/api/alt-banner-5/admin", folder: "banners/alt-banner-5", isMain: false },
};

// Burada bölüm anahtarından konfigürasyona ulaşırken sessiz fallback üretmeden kesin eşleşme sağlıyorum.
export function getBannerSectionConfig(key: BannerSectionKey): BannerSectionConfig {
  return BANNER_SECTION_CONFIGS[key];
}

// Burada dış kaynaktan gelen değerin yalnız belgelenen altı bölüm anahtarından biri olduğunu doğruluyorum.
export function isBannerSectionKey(value: string): value is BannerSectionKey {
  return Object.hasOwn(BANNER_SECTION_CONFIGS, value);
}
