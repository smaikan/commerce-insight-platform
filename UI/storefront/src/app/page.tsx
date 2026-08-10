import Link from "next/link";
import { getPublicBannerSection } from "@/modules/banners/api";
import { AlternateBannerSection, MainBannerSection } from "@/modules/banners/components/banner-sections";
import {
  BANNER_SECTION_CONFIGS,
  BANNER_SECTION_KEYS,
  type BannerSection,
} from "@/modules/banners/types";
import { siteConfig } from "@/lib/site-config";

type LoadedBannerSection = {
  section: BannerSection;
  isMain: boolean;
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
    <div className="min-h-screen bg-white text-zinc-950">
      <header className="border-b border-zinc-200/80 bg-white">
        <div className="mx-auto flex min-h-16 w-full max-w-[90rem] items-center px-4 sm:px-6 lg:px-8">
          <Link href="/" className="rounded-sm text-base font-semibold tracking-[0.18em] text-zinc-950 outline-none focus-visible:ring-2 focus-visible:ring-zinc-950 focus-visible:ring-offset-4">
            {siteConfig.name}
          </Link>
        </div>
      </header>

      <main>
        <h1 className="sr-only">{siteConfig.name} ana sayfa</h1>
        <MainBannerSection section={mainSection} />
        {alternateSections.some((section) => section.items.length > 0) ? (
          <div className="mx-auto w-full max-w-[90rem] px-4 py-4 sm:px-6 sm:py-6 lg:px-8">
            {alternateSections.map((section) => <AlternateBannerSection key={section.key} section={section} />)}
          </div>
        ) : null}
      </main>
    </div>
  );
}
