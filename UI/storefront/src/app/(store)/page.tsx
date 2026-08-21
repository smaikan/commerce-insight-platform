import type { Metadata } from "next";
import Link from "next/link";
import Image from "next/image";

import { siteConfig } from "@/lib/site-config";
import { getPublicBannerSection } from "@/modules/banners/api";
import { AlternateBannerSection, MainBannerSection } from "@/modules/banners/components/banner-sections";
import { StoreBenefits } from "@/modules/home/components/store-benefits";
import {
  BANNER_SECTION_CONFIGS,
  BANNER_SECTION_KEYS,
  type BannerSection,
} from "@/modules/banners/types";
import { getPublishedProducts } from "@/modules/catalog/api";
import { getCollectionShowcase } from "@/modules/catalog/collections";
import { ProductCard } from "@/modules/catalog/components/product-card";
import { CollectionCarousel } from "@/modules/catalog/components/collection-carousel";

type LoadedBannerSection = {
  section: BannerSection;
  isMain: boolean;
};

// Burada ana sayfanın mağaza adı ve sosyal metadata'sını dinamik root sözleşmesinden miras alıp yalnız temiz canonical hedefini tanımlıyorum.
export const metadata: Metadata = {
  alternates: { canonical: "/" },
};

// Burada bağımsız banner endpointlerini ve katalog verilerini paralel başlatıp SEO ve performans standartlarını koruyorum.
export default async function Home() {
  const [bannerResults, bestSellersResult, collectionsResult] = await Promise.all([
    Promise.allSettled(
      BANNER_SECTION_KEYS.map(async (key) => {
        const config = BANNER_SECTION_CONFIGS[key];
        return {
          section: await getPublicBannerSection(key),
          isMain: config.isMain,
        };
      }),
    ),
    getPublishedProducts({ SortBy: 1, PageSize: 4, PageNumber: 1 }).catch(() => null),
    getCollectionShowcase(1, 100).catch(() => null),
  ]);

  const loadedBanners = bannerResults
    .filter((result): result is PromiseFulfilledResult<LoadedBannerSection> => result.status === "fulfilled")
    .map((result) => result.value);
    
  const mainSection = loadedBanners.find((entry) => entry.isMain)?.section;
  // Alternate banners are currently hidden to keep the UI clean as requested, ama Alternate banner sectionlari asagidaki layouta gore eklenebilir.

  return (
    <main id="main-content" className="flex-1 bg-background">
      <h1 className="sr-only">{siteConfig.name} ana sayfa</h1>
      
      {/* 1. Hero Banner */}
      <MainBannerSection section={mainSection} />

      {/* 2. Introduction */}
      <section className="mx-auto w-full max-w-4xl px-4 py-16 text-center sm:px-6 sm:py-24 lg:px-8">
        <h2 className="text-3xl font-semibold tracking-tight text-ink sm:text-5xl">Şıklığın ve Kalitenin Buluşma Noktası</h2>
        <p className="mx-auto mt-6 max-w-2xl text-lg leading-8 text-ink-muted">
          Günlük hayatınıza zarafet katan, özenle seçilmiş birinci sınıf ürün koleksiyonumuzu keşfedin. Kaliteden ödün vermeyen tasarımlarla tarzınızı yansıtın.
        </p>
        <div className="mt-10 flex items-center justify-center gap-x-6">
          <Link
            href="/products"
            className="rounded-xl bg-brand-700 px-8 py-3.5 text-sm font-semibold text-white shadow-sm hover:bg-brand-950 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-brand-700 transition-colors"
          >
            Kataloğu Keşfet
          </Link>
        </div>
      </section>

      {/* 3. Shop Best Sellers */}
      {bestSellersResult?.items?.length ? (
        <section className="mx-auto w-full max-w-[90rem] px-4 py-12 sm:px-6 lg:px-8">
          <div className="flex items-center justify-between border-b border-line pb-6">
            <h2 className="text-2xl font-semibold tracking-tight text-ink uppercase">En Çok Satanlar</h2>
            <Link href="/products?sort=popular" className="text-sm font-semibold text-brand-700 hover:text-brand-950">
              Tümünü gör <span aria-hidden="true">&rarr;</span>
            </Link>
          </div>
          <div className="mt-8 grid grid-cols-2 gap-x-4 gap-y-10 sm:gap-x-6 md:grid-cols-4 lg:gap-x-8">
            {bestSellersResult.items.map((product) => (
              <ProductCard key={product.id} product={product} />
            ))}
          </div>
        </section>
      ) : null}

      {/* 4. Our Collection */}
      {collectionsResult?.items?.length ? (
        <section className="mx-auto w-full max-w-[90rem] px-4 py-12 sm:px-6 lg:px-8">
          <div className="flex items-center justify-between border-b border-line pb-6">
            <h2 className="text-2xl font-semibold tracking-tight text-ink uppercase">Koleksiyonlarımız</h2>
          </div>
          <CollectionCarousel collections={collectionsResult.items} />
        </section>
      ) : null}

      {/* 5. Store Benefits */}
      <StoreBenefits />
    </main>
  );
}
