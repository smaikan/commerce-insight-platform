import "server-only";

import { siteConfig } from "../../lib/site-config";
import { getBannerSectionConfig } from "./section-config";
import { normalizePublicBannerSection } from "./transform";
import type { BannerSection, BannerSectionKey } from "./types";

export const PUBLIC_BANNER_REVALIDATE_SECONDS = 60;

// Burada bölüm yolunu tek origin altında birleştirerek test edilebilir ve kesin endpoint URL'si üretiyorum.
export function publicBannerSectionUrl(key: BannerSectionKey, apiUrl = siteConfig.apiUrl): string {
  return new URL(getBannerSectionConfig(key).publicPath, `${apiUrl}/`).toString();
}

// Burada public banner cevabını kısa süreli önbellekle alıyor, HTTP ve JSON hatalarını üst katmana bırakıyorum.
export async function getPublicBannerSection(key: BannerSectionKey): Promise<BannerSection> {
  const response = await fetch(publicBannerSectionUrl(key), {
    headers: { Accept: "application/json" },
    next: {
      revalidate: PUBLIC_BANNER_REVALIDATE_SECONDS,
      tags: ["banners", "banner-sections", `banner-section:${key}`],
    },
  });

  if (!response.ok) {
    throw new Error(`${getBannerSectionConfig(key).label} alınamadı (${response.status}).`);
  }

  const section = (await response.json()) as BannerSection;
  return normalizePublicBannerSection(key, section);
}
