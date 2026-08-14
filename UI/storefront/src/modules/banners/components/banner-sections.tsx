import Image from "next/image";
import type { BannerSection, BannerSectionItem } from "@/modules/banners/types";

type BannerMediaProps = {
  item: BannerSectionItem;
  priority?: boolean;
  variant: "main" | "main-secondary" | "alternate";
};

// Burada seçili ana bannerı ilk sıraya alıp kalan aktif kayıtları erişilebilir yatay seçki olarak sunuyorum.
export function MainBannerSection({ section }: { section?: BannerSection | null }) {
  if (!section?.items.length) return null;
  const items = orderMainItems(section.items);
  const [primary, ...secondary] = items;

  return (
    <section aria-labelledby="main-banner-heading" className="mx-auto w-full max-w-[90rem] px-4 pt-4 sm:px-6 sm:pt-6 lg:px-8">
      <h2 id="main-banner-heading" className="sr-only">{section.name}</h2>
      <BannerMedia item={primary} priority variant="main" />
      {secondary.length ? (
        <ul aria-label={`${section.name} diğer içerikleri`} className="mt-3 flex snap-x snap-mandatory gap-3 overflow-x-auto pb-2 [scrollbar-width:thin]">
          {secondary.map((item) => (
            <li key={item.id} className="w-[78%] shrink-0 snap-start sm:w-[48%] lg:w-[32%]">
              <BannerMedia item={item} variant="main-secondary" />
            </li>
          ))}
        </ul>
      ) : null}
    </section>
  );
}

// Burada her alt banner bölümünü kendi semantik ve görsel sınırında tutup boş bölümler için container oluşturmuyorum.
export function AlternateBannerSection({ section }: { section?: BannerSection | null }) {
  if (!section?.items.length) return null;
  const gridClass = section.items.length === 1
    ? "grid-cols-1"
    : section.items.length === 2
      ? "grid-cols-1 sm:grid-cols-2"
      : "grid-cols-1 sm:grid-cols-2 lg:grid-cols-3";

  return (
    <section aria-labelledby={`banner-heading-${section.key}`} className="border-t border-zinc-200/80 py-6 first:border-t-0 sm:py-8">
      <h2 id={`banner-heading-${section.key}`} className="sr-only">{section.name}</h2>
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
  const frameClass = variant === "main"
    ? "aspect-[16/11] sm:aspect-[16/7] lg:aspect-[21/8]"
    : "aspect-video";
  const media = item.mediaType === 2 ? (
    <video
      className="size-full object-cover"
      src={item.mediaUrl}
      aria-label={item.altText || item.name}
      controls
      muted
      playsInline
      preload={variant === "main" ? "metadata" : "none"}
    />
  ) : (
    <Image
      className="object-cover"
      src={item.mediaUrl}
      alt={item.altText || ""}
      fill
      loading={priority ? "eager" : "lazy"}
      fetchPriority={priority ? "high" : undefined}
      sizes={variant === "main" ? "(min-width: 1440px) 1440px, 100vw" : "(min-width: 1024px) 32vw, (min-width: 640px) 48vw, 78vw"}
    />
  );

  if (item.mediaType === 2) {
    return (
      <div className={`relative overflow-hidden rounded-xl bg-surface-subtle ${frameClass}`}>
        {media}
        {href ? (
          <a href={href} className="absolute right-3 top-3 inline-flex min-h-10 items-center rounded-lg bg-white/95 px-3 text-sm font-semibold text-zinc-950 shadow-sm outline-none hover:bg-white focus-visible:ring-2 focus-visible:ring-zinc-950 focus-visible:ring-offset-2" aria-label={`${item.altText || item.name}: içeriğe git`}>
            İçeriğe git
          </a>
        ) : null}
      </div>
    );
  }

  return href ? (
    <a href={href} className={`relative block overflow-hidden rounded-xl bg-surface-subtle outline-none focus-visible:ring-2 focus-visible:ring-brand-700 focus-visible:ring-offset-2 ${frameClass}`} aria-label={item.altText || item.name}>
      {media}
    </a>
  ) : (
    <div className={`relative overflow-hidden rounded-xl bg-surface-subtle ${frameClass}`}>{media}</div>
  );
}

// Burada backend sırası bozulsa bile seçili main kaydını ilk, diğer kayıtları displayOrder sırasında tutuyorum.
function orderMainItems(items: BannerSectionItem[]): BannerSectionItem[] {
  return [...items].sort((left, right) => Number(right.isMain) - Number(left.isMain) || left.displayOrder - right.displayOrder);
}

// Burada yalnız uygulama içi yollarla HTTP/HTTPS hedeflerini bağlantı olarak kabul ediyorum.
function safeTargetUrl(value?: string | null): string | undefined {
  const target = value?.trim();
  if (!target) return undefined;
  if (target.startsWith("/") && !target.startsWith("//")) return target;
  try {
    const parsed = new URL(target);
    return parsed.protocol === "http:" || parsed.protocol === "https:" ? parsed.toString() : undefined;
  } catch {
    return undefined;
  }
}
