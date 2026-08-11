import type { Metadata } from "next";

import { siteConfig } from "@/lib/site-config";
import { getPublicBannerSection } from "@/modules/banners/api";
import { AlternateBannerSection, MainBannerSection } from "@/modules/banners/components/banner-sections";
import {
  BANNER_SECTION_CONFIGS,
  BANNER_SECTION_KEYS,
  type BannerSection,
} from "@/modules/banners/types";

type LoadedBannerSection = {
  section: BannerSection;
  isMain: boolean;
};

// Burada ana sayfanın temiz canonical ve aynı sayfa niyetini taşıyan sosyal metadata değerlerini tanımlıyorum.
export const metadata: Metadata = {
  title: { absolute: siteConfig.name },
  description: siteConfig.description,
  alternates: { canonical: "/" },
  openGraph: {
    type: "website",
    url: "/",
    title: siteConfig.name,
    description: siteConfig.description,
  },
};

// Burada bağımsız banner endpointlerini paralel başlatıp tek bölüm hatasının ana sayfayı çökertmesini engelliyorum.
export default async function Home() {
  const results = await Promise.allSettled(
    BANNER_SECTION_KEYS.map(async (key) => {
      const config = BANNER_SECTION_CONFIGS[key];
      return {
        section: await getPublicBannerSection(key),
        isMain: config.isMain,
      };
    }),
  );
  const loaded = results
    .filter((result): result is PromiseFulfilledResult<LoadedBannerSection> => result.status === "fulfilled")
    .map((result) => result.value);
  const mainSection = loaded.find((entry) => entry.isMain)?.section;
  const alternateSections = loaded.filter((entry) => !entry.isMain).map((entry) => entry.section);

  return (
    <main id="main-content" className="flex-1 bg-background">
      <h1 className="sr-only">{siteConfig.name} ana sayfa</h1>
      <MainBannerSection section={mainSection} />
      {alternateSections.some((section) => section.items.length > 0) ? (
        <div className="page-shell py-4 sm:py-6">
          {alternateSections.map((section) => <AlternateBannerSection key={section.key} section={section} />)}
        </div>
      ) : null}
    </main>
  );
}
