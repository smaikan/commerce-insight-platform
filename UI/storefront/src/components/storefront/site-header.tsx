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
      {/* Burada mağaza adını kaldırıp hizmet duyurularını JavaScript gerektirmeyen, kesintisiz kayan ve duraklatılabilir bir şeritte sunuyorum. */}
      <div className="bg-brand-950 text-white">
        <div className="announcement-strip relative overflow-hidden" role="region" aria-label="Mağaza duyuruları">
          {/* Burada CSS checkbox durumunu küçük bir hareket kontrolüne bağlayarak kullanıcıya kalıcı duraklatma seçeneği veriyorum. */}
          <input
            id="announcement-motion-toggle"
            type="checkbox"
            className="announcement-motion-toggle peer sr-only"
            aria-label="Duyuru hareketini duraklat veya başlat"
          />
          <div className="announcement-marquee-track flex w-max">
            <AnnouncementList />
            {/* Burada yalnız görsel süreklilik sağlayan ikinci kopyayı ekran okuyucudan gizliyorum. */}
            <AnnouncementList duplicate />
          </div>
          <label
            htmlFor="announcement-motion-toggle"
            className="announcement-motion-control absolute inset-y-0 right-0 flex w-10 cursor-pointer items-center justify-center border-l border-footer-line bg-brand-950 text-white peer-focus-visible:ring-2 peer-focus-visible:ring-brand-600 peer-focus-visible:ring-inset"
          >
            <svg aria-hidden="true" viewBox="0 0 20 20" className="announcement-pause-icon size-3.5" fill="currentColor"><path d="M5 4h3v12H5zm7 0h3v12h-3z" /></svg>
            <svg aria-hidden="true" viewBox="0 0 20 20" className="announcement-play-icon hidden size-3.5" fill="currentColor"><path d="m6 4 9 6-9 6z" /></svg>
          </label>
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
    <ul className={`announcement-marquee-group flex min-h-9 shrink-0 items-center justify-around gap-10 px-8 pr-14 text-[0.625rem] font-semibold tracking-[0.08em] whitespace-nowrap uppercase lg:text-[0.6875rem] lg:tracking-[0.1em] ${duplicate ? "announcement-marquee-copy" : ""}`} aria-hidden={duplicate || undefined}>
      {STORE_ANNOUNCEMENTS.map((announcement) => (
        // Burada duyuruları başlarında dekoratif nokta olmadan doğrudan, temiz bir metin ritmiyle gösteriyorum.
        <li key={announcement} className="shrink-0">{announcement}</li>
      ))}
    </ul>
  );
}
