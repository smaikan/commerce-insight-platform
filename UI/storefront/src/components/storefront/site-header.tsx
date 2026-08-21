import Image from "next/image";
import Link from "next/link";

import { CartIndicator } from "@/components/storefront/cart-indicator";
import { DesktopNavigation } from "@/components/storefront/desktop-navigation";
import { MobileNavigation } from "@/components/storefront/mobile-navigation";
import { ScrollAwareHeader } from "@/components/storefront/scroll-aware-header";
import { siteConfig } from "@/lib/site-config";
import { DesktopAuthNavigation } from "@/modules/auth/components/header-auth-navigation";
import { getStorefrontNavigation } from "@/modules/catalog/navigation";
import { FavoritesIndicator } from "@/modules/favorites/components/favorites-indicator";
import { SearchOverlay } from "@/modules/search/components/search-overlay";
import { getPublicStoreSettings } from "@/modules/store-settings/api";
import type { PublicStoreSettings } from "@/modules/store-settings/types";
import { safeStoreSettingsUrl } from "@/modules/store-settings/url";

type HeaderSettings = Pick<PublicStoreSettings, "displayName" | "logoUrl">;

// Burada mağaza ayarları okunamadığında header'ın kullanılabilir metin markasıyla çalışmaya devam etmesini sağlıyorum.
const FALLBACK_HEADER_SETTINGS: HeaderSettings = {
  displayName: siteConfig.name,
  logoUrl: null,
};

// Burada kullanıcı tarafından onaylanan mağaza taahhütlerini tek ve kolay güncellenebilir duyuru listesinde tutuyorum.
const STORE_ANNOUNCEMENTS = [
  "Ücretsiz Kargo",
  "Vade Farksız 3 Taksit",
  "Aynı Gün Kargo",
  "Kolay İade",
  "7/24 Canlı Destek",
] as const;

// Burada masaüstü ve mobilde aynı bilgi mimarisini koruyan hafif Storefront üst alanını oluşturuyorum.
export async function SiteHeader() {
  // Burada navigasyon ve mağaza kimliğini birbirini bekletmeden okuyup ortak header'a taşıyorum.
  const [navigationGroups, settings] = await Promise.all([
    getStorefrontNavigation(),
    getPublicStoreSettings().catch(() => FALLBACK_HEADER_SETTINGS),
  ]);
  const displayName = settings.displayName.trim() || siteConfig.name;
  const logoUrl = safeStoreSettingsUrl(settings.logoUrl);
  // Burada marka facetini katalog filtrelerinde korurken header bilgi mimarisinden tek noktada çıkarıyorum.
  const headerNavigationGroups = navigationGroups.filter((group) => group.id !== "brands");

  return (
    <>
      {/* Burada hizmet duyurularını kesintisiz kayan bir şeritte sunup sağ tarafına İletişim linkini sabitliyorum. */}
      <div className="bg-brand-950 text-white border-b border-brand-900/60">
        <div className="flex items-stretch">
          <div className="announcement-strip relative flex-1 min-w-0 overflow-hidden" role="region" aria-label="Mağaza duyuruları">
            <div className="announcement-marquee-track flex w-max">
              <AnnouncementList />
              {/* Burada yalnız görsel süreklilik sağlayan ikinci kopyayı ekran okuyucudan gizliyorum. */}
              <AnnouncementList duplicate />
            </div>
          </div>
          <div className="relative z-10 shrink-0 flex items-stretch border-l border-white/10 bg-brand-950">
            <Link
              href="/contact"
              prefetch={false}
              className="focus-ring flex items-center gap-1.5 px-3.5 sm:px-5 text-[0.625rem] sm:text-[0.6875rem] font-bold tracking-[0.1em] text-white hover:text-brand-300 transition-colors uppercase whitespace-nowrap"
            >
              <svg className="size-3 text-brand-400" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                <path strokeLinecap="round" strokeLinejoin="round" d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
              </svg>
              <span>İletişim</span>
            </Link>
          </div>
        </div>
      </div>
      <ScrollAwareHeader>
        {/* Burada mobil aksiyonları dış kenara yaklaştırıp daha geniş ekranlarda kümeleri logoya dengeli biçimde yaklaştırıyorum. */}
        <div className="page-shell relative flex min-h-22 items-center sm:min-h-24 sm:px-2 lg:px-4 xl:px-6">
          {/* Burada mobil menü tetikleyicisini ve masaüstü menü itemlerini header'ın sol kümesinde tutuyorum. */}
          <div className="relative z-20 flex min-w-0 items-center lg:w-[calc(50%_-_3rem)] lg:flex-none">
            <MobileNavigation siteName={displayName} groups={headerNavigationGroups} />
            <DesktopNavigation groups={headerNavigationGroups} />
          </div>

          <Link
            href="/"
            prefetch={false}
            className="focus-ring absolute left-1/2 z-10 inline-flex max-w-[7rem] shrink-0 -translate-x-1/2 items-center sm:max-w-[10rem]"
            aria-label={`${displayName} ana sayfa`}
          >
            {/* Burada logoyu yan kümelerin genişliğinden bağımsız biçimde header'ın gerçek yatay merkezine sabitliyorum. */}
            {logoUrl ? (
              <Image
                src={logoUrl}
                alt={displayName}
                width={300}
                height={300}
                sizes="(min-width: 640px) 80px, 48px"
                loading="eager"
                className="size-12 object-contain sm:size-20"
              />
            ) : (
              <span className="truncate text-base font-black tracking-[0.14em] text-brand-950 sm:text-lg sm:tracking-[0.16em]">{displayName}</span>
            )}
          </Link>

          <div className="relative z-20 -mr-2 ml-auto flex shrink-0 items-center justify-end text-sm font-semibold sm:mr-0">
            {/* Burada büyüteç tetikleyicisini hesap ve sepet aksiyonlarıyla aynı dokunma hedefinde konumlandırıyorum. */}
            <SearchOverlay />
            {/* Burada favoriler hedefini arama ve hesap aksiyonları arasında tek bakışta erişilebilir bir kalp olarak sunuyorum. */}
            <FavoritesIndicator />
            {/* Burada oturum durumuna göre guest aksiyonlarını veya Hesabım menüsünü aynı sabit navbar alanında gösteriyorum. */}
            <DesktopAuthNavigation />
              <div className="ml-0.5 border-l border-line pl-0.5 sm:ml-2 sm:pl-1.5">
                <CartIndicator />
              </div>
          </div>
        </div>
      </ScrollAwareHeader>
    </>
  );
}

// Burada aynı duyuru grubunu erişilebilir ana liste ve görsel devam kopyası için tek kaynaktan üretiyorum.
function AnnouncementList({ duplicate = false }: { duplicate?: boolean }) {
  return (
    <ul className={`announcement-marquee-group flex min-h-9 shrink-0 items-center justify-around gap-10 px-6 sm:px-10 text-[0.625rem] font-semibold tracking-[0.08em] whitespace-nowrap uppercase lg:text-[0.6875rem] lg:tracking-[0.1em] ${duplicate ? "announcement-marquee-copy" : ""}`} aria-hidden={duplicate || undefined}>
      {STORE_ANNOUNCEMENTS.map((announcement) => (
        // Burada duyuruları başlarında dekoratif nokta olmadan doğrudan, temiz bir metin ritmiyle gösteriyorum.
        <li key={announcement} className="shrink-0">{announcement}</li>
      ))}
    </ul>
  );
}
