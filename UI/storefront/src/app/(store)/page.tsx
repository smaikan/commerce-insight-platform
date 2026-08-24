import type { Metadata } from "next";
import Link from "next/link";

import { siteConfig } from "@/lib/site-config";
import { getPublicBannerSection } from "@/modules/banners/api";
import { MainBannerSection } from "@/modules/banners/components/banner-sections";
import { StoreBenefits } from "@/modules/home/components/store-benefits";
import { getPublishedProducts } from "@/modules/catalog/api";
import { getCategoryShowcase, getMostPopulatedCategories } from "@/modules/catalog/categories";
import { getCollectionShowcase, getMostPopulatedCollections } from "@/modules/catalog/collections";
import { CollectionCarousel } from "@/modules/catalog/components/collection-carousel";
import { BrandStory } from "@/modules/home/components/editorial-showcase";
import { CategoryPillsBar } from "@/modules/home/components/category-pills-bar";
import { PromoRibbon } from "@/modules/home/components/promo-ribbon";
import { FeaturedProductsTabs } from "@/modules/home/components/featured-products-tabs";
import { HomeBentoCategories } from "@/modules/home/components/home-bento-categories";
import { HomeCuratedCollections } from "@/modules/home/components/home-curated-collections";
import { LookbookBanner } from "@/modules/home/components/lookbook-banner";
import { CraftsmanshipStrip } from "@/modules/home/components/craftsmanship-strip";
import { InstagramGallerySection } from "@/modules/home/components/instagram-gallery-section";
import { CustomerReviewsSection } from "@/modules/home/components/customer-reviews-section";
import { NewsletterSection } from "@/modules/home/components/newsletter-section";

// Burada ana sayfanın mağaza adı ve canonical bilgisini yapılandırıyorum.
export const metadata: Metadata = {
  alternates: { canonical: "/" },
};

// Burada ana sayfanın her istekte canlı API verileriyle eksiksiz ve güncel render edilmesini sağlıyorum.
export const dynamic = "force-dynamic";
export const revalidate = 0;

// Burada ana sayfa için gerekli tüm vitrin, ürün, kategori ve koleksiyon verilerini paralel olarak yüklüyorum.
export default async function Home() {
  const [
    mainSection,
    mainMobileSection,
    altBannerOne,
    bestSellersResult,
    newArrivalsResult,
    allCategoriesResult,
    collectionsResult,
    bentoCategories,
    curatedCollections,
  ] = await Promise.all([
    getPublicBannerSection("main-banner").catch(() => null),
    getPublicBannerSection("main-banner-mobile").catch(() => null),
    getPublicBannerSection("alt-banner-1").catch(() => null),
    getPublishedProducts({ SortBy: 1, PageSize: 8, PageNumber: 1 }).catch(() => null),
    getPublishedProducts({ SortBy: 2, PageSize: 8, PageNumber: 1 }).catch(() => null),
    getCategoryShowcase(1, 100).catch(() => null),
    getCollectionShowcase(1, 100).catch(() => null),
    getMostPopulatedCategories(5).catch(() => []),
    getMostPopulatedCollections(3).catch(() => []),
  ]);

  const storyImage = altBannerOne?.items.find((item) => item.mediaType === 1);
  const bestSellers = bestSellersResult?.items ?? [];
  const newArrivals = newArrivalsResult?.items ?? [];
  const allCategories = allCategoriesResult?.items ?? [];
  const allCollections = collectionsResult?.items ?? [];

  return (
    <main id="main-content" className="flex-1 bg-background">
      <h1 className="sr-only">{siteConfig.name} ana sayfa</h1>

      {/* 1. Hero Banner (Masaüstü & Mobil Ayrılmış Responsive Banner) */}
      <MainBannerSection
        desktopSection={mainSection}
        mobileSection={mainMobileSection}
      />

      {/* 2. Alışveriş Ayrıcalıkları Şeridi */}
      <PromoRibbon />

      {/* 3. Hızlı Kategori Keşif Barı (Story / Pill) */}
      <CategoryPillsBar categories={allCategories} />

      {/* 4. Sekmeli Popüler Ürünler Vitrini (En Çok Satanlar & Yeni Gelenler) */}
      <FeaturedProductsTabs
        bestSellers={bestSellers}
        newArrivals={newArrivals}
      />

      {/* 5. Asimetrik Bento Grid Kategori Vitrini */}
      <HomeBentoCategories categories={bentoCategories} />

      {/* 6. Lookbook & Sezonluk Editoryal Stil Banner'ı (Shoppable Hotspot) */}
      <LookbookBanner />

      {/* 7. Özel Tematik Koleksiyonlar Vitrini (3'lü Editoryal Kartlar) */}
      <HomeCuratedCollections collections={curatedCollections} />

      {/* 8. Koleksiyonlarımız Yatay Kaydırılabilir Rayı */}
      {allCollections.length > 0 ? (
        <section
          aria-labelledby="collections-carousel-heading"
          className="home-shell py-10 sm:py-14 border-t border-line/60"
        >
          {/* Koleksiyon başlığı ile keşif bağlantısını mobilde rahat okunacak ayrı satırlarda sunuyorum. */}
          <div className="flex flex-col items-start gap-3 border-b border-line pb-4 sm:flex-row sm:items-end sm:justify-between">
            <div>
              <p className="text-xs font-bold uppercase tracking-[0.18em] text-brand-700">
                TÜM KOLEKSİYONLAR
              </p>
              <h2
                id="collections-carousel-heading"
                className="mt-1 text-2xl font-bold tracking-tight text-ink sm:text-3xl"
              >
                Koleksiyonlarımızı Keşfedin
              </h2>
            </div>
            <Link
              href="/collections"
              prefetch={false}
              className="focus-ring inline-flex items-center gap-1 text-xs font-semibold text-brand-700 transition-colors hover:text-brand-950 sm:text-sm"
            >
              Tüm Koleksiyonlar <span aria-hidden="true">&rarr;</span>
            </Link>
          </div>
          <CollectionCarousel collections={allCollections} />
        </section>
      ) : null}

      {/* 9. Lüks Malzeme & İşçilik Standartları */}
      <CraftsmanshipStrip />

      {/* 10. Marka Hikayesi (Varsa Alt Banner 1) */}
      <BrandStory image={storyImage} />

      {/* 11. Stil İlhamı & Instagram Topluluk Galerisi */}
      <InstagramGallerySection />

      {/* 12. Müşteri Deneyimi & Değerlendirmeler */}
      <CustomerReviewsSection />

      {/* 13. VIP Bülten & Özel Fırsatlar Kulübü */}
      <NewsletterSection />

      {/* 14. Mağaza Alışveriş Güvencesi */}
      <StoreBenefits />
    </main>
  );
}
