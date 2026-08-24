import Image from "next/image";
import type { BannerSection, BannerSectionItem } from "@/modules/banners/types";
import { HeroBannerCarousel } from "./hero-banner-carousel";

export type ResponsiveBannerSlide = {
  id: string;
  desktopItem: BannerSectionItem;
  mobileItem?: BannerSectionItem;
};

type BannerMediaProps = {
  item: BannerSectionItem;
  priority?: boolean;
  variant: "main" | "mobile-main" | "main-secondary" | "alternate";
};

type MainBannerSectionProps = {
  desktopSection?: BannerSection | null;
  mobileSection?: BannerSection | null;
  section?: BannerSection | null;
};

// Burada masaüstü ve mobil banner bölümlerini tek bir responsive karuselde birleştirip HTML içinde picture etiketiyle yalnız aktif ekran görselinin indirilmesini sağlıyorum (LCP optimizasyonu).
export function MainBannerSection({
  desktopSection,
  mobileSection,
  section,
}: MainBannerSectionProps) {
  const desktop = desktopSection ?? section;
  const mobile = mobileSection ?? desktop;

  const desktopItems = desktop?.items?.length ? orderMainItems(desktop.items) : [];
  const mobileItems = mobile?.items?.length ? orderMainItems(mobile.items) : [];

  if (desktopItems.length === 0 && mobileItems.length === 0) return null;

  const maxLen = Math.max(desktopItems.length, mobileItems.length);
  const slides: ResponsiveBannerSlide[] = [];

  for (let i = 0; i < maxLen; i++) {
    const desktopItem = desktopItems[i] || desktopItems[0] || mobileItems[i];
    const mobileItem = mobileItems[i] || mobileItems[0] || desktopItem;
    if (desktopItem || mobileItem) {
      slides.push({
        id: `slide-${i}-${desktopItem?.id || mobileItem?.id}`,
        desktopItem: desktopItem || mobileItem!,
        mobileItem: mobileItem || desktopItem,
      });
    }
  }

  return (
    <section aria-labelledby="main-banner-heading" className="w-full">
      <h2 id="main-banner-heading" className="sr-only">
        {desktop?.name || mobile?.name || "Main Banner"}
      </h2>
      <HeroBannerCarousel slides={slides} />
    </section>
  );
}

// Burada duyarlı banner slaydını render edip picture ve media query ile mobilde yalnız mobil görseli, masaüstünde yalnız masaüstü görseli indiriyorum.
export function ResponsiveBannerSlideView({
  slide,
  priority = false,
}: {
  slide: ResponsiveBannerSlide;
  priority?: boolean;
}) {
  const desktopItem = slide.desktopItem;
  const mobileItem = slide.mobileItem || desktopItem;
  const href = safeTargetUrl(mobileItem.targetUrl || desktopItem.targetUrl);

  const isVideo = desktopItem.mediaType === 2 || mobileItem.mediaType === 2;

  if (isVideo) {
    return (
      <div className="relative aspect-square md:aspect-auto md:h-[75vh] w-full overflow-hidden bg-surface-subtle">
        <video
          className="size-full object-cover"
          src={mobileItem.mediaUrl || desktopItem.mediaUrl}
          aria-label={mobileItem.altText || desktopItem.altText || mobileItem.name}
          controls
          muted
          playsInline
          preload={priority ? "metadata" : "none"}
        />
        {href ? (
          <a
            draggable={false}
            href={href}
            className="absolute right-3 top-3 inline-flex min-h-10 items-center rounded-lg bg-white/95 px-3 text-sm font-semibold text-zinc-950 shadow-sm outline-none hover:bg-white focus-visible:ring-2 focus-visible:ring-zinc-950 focus-visible:ring-offset-2 cursor-pointer"
            aria-label={`${mobileItem.altText || mobileItem.name}: içeriğe git`}
          >
            İçeriğe git
          </a>
        ) : null}
      </div>
    );
  }

  const desktopUrl = desktopItem.mediaUrl;
  const mobileUrl = mobileItem.mediaUrl;
  const altText = mobileItem.altText || desktopItem.altText || mobileItem.name || "";

  const isDifferent = desktopUrl && mobileUrl && desktopUrl !== mobileUrl;

  const content = (
    <div className="relative aspect-square md:aspect-auto md:h-[75vh] w-full overflow-hidden bg-surface-subtle">
      {isDifferent ? (
        <picture className="block size-full">
          <source
            media="(min-width: 768px)"
            srcSet={`/_next/image?url=${encodeURIComponent(desktopUrl)}&w=1200&q=75 1200w, /_next/image?url=${encodeURIComponent(desktopUrl)}&w=1920&q=75 1920w, /_next/image?url=${encodeURIComponent(desktopUrl)}&w=2048&q=75 2048w`}
            sizes="100vw"
          />
          <img
            src={`/_next/image?url=${encodeURIComponent(mobileUrl)}&w=750&q=75`}
            srcSet={`/_next/image?url=${encodeURIComponent(mobileUrl)}&w=640&q=75 640w, /_next/image?url=${encodeURIComponent(mobileUrl)}&w=750&q=75 750w, /_next/image?url=${encodeURIComponent(mobileUrl)}&w=828&q=75 828w`}
            sizes="100vw"
            alt={altText}
            className="size-full object-cover select-none"
            loading={priority ? "eager" : "lazy"}
            fetchPriority={priority ? "high" : undefined}
            decoding="async"
            draggable={false}
          />
        </picture>
      ) : (
        <Image
          className="object-cover select-none"
          src={mobileUrl || desktopUrl}
          alt={altText}
          fill
          draggable={false}
          loading={priority ? "eager" : "lazy"}
          fetchPriority={priority ? "high" : undefined}
          sizes="(min-width: 768px) 100vw, 100vw"
          quality={75}
        />
      )}
    </div>
  );

  if (href) {
    return (
      <a
        draggable={false}
        href={href}
        className="relative block size-full overflow-hidden outline-none focus-visible:ring-2 focus-visible:ring-brand-700 focus-visible:ring-offset-2 cursor-pointer"
        aria-label={altText}
      >
        {content}
      </a>
    );
  }

  return content;
}

// Burada her alt banner bölümünü kendi semantik ve görsel sınırında tutup boş bölümler için container oluşturmuyorum.
export function AlternateBannerSection({ section }: { section?: BannerSection | null }) {
  if (!section?.items.length) return null;
  const gridClass =
    section.items.length === 1
      ? "grid-cols-1"
      : section.items.length === 2
        ? "grid-cols-1 sm:grid-cols-2"
        : "grid-cols-1 sm:grid-cols-2 lg:grid-cols-3";

  return (
    <section
      aria-labelledby={`banner-heading-${section.key}`}
      className="border-t border-zinc-200/80 py-6 first:border-t-0 sm:py-8"
    >
      <h2 id={`banner-heading-${section.key}`} className="sr-only">
        {section.name}
      </h2>
      <ul className={`grid ${gridClass} gap-4`}>
        {section.items.map((item) => (
          <li key={item.id}>
            <BannerMedia item={item} variant="alternate" />
          </li>
        ))}
      </ul>
    </section>
  );
}

// Burada banner medyasını sabit oranlı yüzeyde resim veya kontrollü video olarak render ediyorum.
export function BannerMedia({ item, priority = false, variant }: BannerMediaProps) {
  const href = safeTargetUrl(item.targetUrl);
  const frameClass =
    variant === "main"
      ? "h-[75vh] w-full"
      : variant === "mobile-main"
        ? "aspect-square w-full"
        : "aspect-video";

  const media =
    item.mediaType === 2 ? (
      <video
        className="size-full object-cover"
        src={item.mediaUrl}
        aria-label={item.altText || item.name}
        controls
        muted
        playsInline
        preload={variant === "main" || variant === "mobile-main" ? "metadata" : "none"}
      />
    ) : (
      <Image
        className="object-cover"
        src={item.mediaUrl}
        alt={item.altText || ""}
        fill
        draggable={false}
        loading={priority ? "eager" : "lazy"}
        fetchPriority={priority ? "high" : undefined}
        sizes={
          variant === "main" || variant === "mobile-main"
            ? "100vw"
            : "(min-width: 1024px) 32vw, (min-width: 640px) 48vw, 78vw"
        }
      />
    );

  const roundedClass =
    variant === "main" || variant === "mobile-main" ? "rounded-none" : "rounded-xl";

  if (item.mediaType === 2) {
    return (
      <div className={`relative overflow-hidden ${roundedClass} bg-surface-subtle ${frameClass}`}>
        {media}
        {href ? (
          <a
            draggable={false}
            href={href}
            className="absolute right-3 top-3 inline-flex min-h-10 items-center rounded-lg bg-white/95 px-3 text-sm font-semibold text-zinc-950 shadow-sm outline-none hover:bg-white focus-visible:ring-2 focus-visible:ring-zinc-950 focus-visible:ring-offset-2 cursor-pointer"
            aria-label={`${item.altText || item.name}: içeriğe git`}
          >
            İçeriğe git
          </a>
        ) : null}
      </div>
    );
  }

  return href ? (
    <a
      draggable={false}
      href={href}
      className={`relative block overflow-hidden ${roundedClass} bg-surface-subtle outline-none focus-visible:ring-2 focus-visible:ring-brand-700 focus-visible:ring-offset-2 cursor-pointer ${frameClass}`}
      aria-label={item.altText || item.name}
    >
      {media}
    </a>
  ) : (
    <div className={`relative overflow-hidden ${roundedClass} bg-surface-subtle ${frameClass}`}>
      {media}
    </div>
  );
}

// Burada backend sırası bozulsa bile seçili main kaydını ilk, diğer kayıtları displayOrder sırasında tutuyorum.
function orderMainItems(items: BannerSectionItem[]): BannerSectionItem[] {
  return [...items].sort(
    (left, right) =>
      Number(right.isMain) - Number(left.isMain) || left.displayOrder - right.displayOrder,
  );
}

// Burada yalnız güvenli uygulama içi yollarla HTTP/HTTPS hedeflerini bağlantı olarak kabul ediyorum.
function safeTargetUrl(value?: string | null): string | undefined {
  const target = value?.trim();
  if (!target) return undefined;
  if (target.startsWith("http://") || target.startsWith("https://")) return target;
  if (target.startsWith("/") && !target.startsWith("//")) return target;
  if (!target.includes(":") && !target.startsWith("//")) return `/${target}`;

  return undefined;
}
