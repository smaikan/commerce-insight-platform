import type { components } from "../../generated/api";

// Burada banner alan tiplerini elle çoğaltmadan üretilen OpenAPI sözleşmesine bağlıyorum.
export type BannerSection = components["schemas"]["BannerSectionDto"];
export type BannerSectionItem = components["schemas"]["BannerItemDto"];
export type BannerMediaType = components["schemas"]["BannerMediaType"];

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

// Burada sayfa kompozisyonunun tip ve sabitleri tek modül girişinden alabilmesi için bölüm tablosunu yeniden dışa aktarıyorum.
export { BANNER_SECTION_CONFIGS, BANNER_SECTION_KEYS } from "./section-config";
