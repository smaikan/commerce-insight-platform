import Image from "next/image";
import Link from "next/link";

import type { BannerSectionItem } from "@/modules/banners/types";

export type HomeEditorialItem = {
  id: string;
  name: string;
  href: string;
  imageUrl?: string | null;
  imageAlt: string;
  productCount: number;
};

type EditorialPairProps = {
  id: string;
  eyebrow: string;
  title: string;
  description: string;
  allHref: string;
  allLabel: string;
  items: HomeEditorialItem[];
  compactMediaCorners?: boolean;
};

// Burada kategori ve koleksiyon çiftlerini tamamen server-rendered bağlantılar olarak sunuyorum.
export function EditorialPair({
  id,
  eyebrow,
  title,
  description,
  allHref,
  allLabel,
  items,
  compactMediaCorners = false,
}: EditorialPairProps) {
  if (items.length === 0) return null;

  return (
    <section id={id} className="home-shell scroll-mt-24 py-8 sm:py-10 lg:py-12" aria-labelledby={`${id}-title`}>
      <div className="border-b border-line pb-6 sm:flex sm:items-end sm:justify-between sm:gap-10">
        <div className="max-w-2xl">
          <p className="text-xs font-bold uppercase tracking-[0.18em] text-brand-700">{eyebrow}</p>
          <h2 id={`${id}-title`} className="mt-2 text-2xl font-semibold tracking-[-0.025em] text-brand-950 sm:text-3xl">
            {title}
          </h2>
          <p className="mt-3 text-sm leading-6 text-ink-muted sm:text-base">{description}</p>
        </div>
        <Link href={allHref} className="focus-ring mt-5 inline-flex min-h-11 shrink-0 items-center gap-2 text-sm font-bold text-brand-700 hover:text-brand-950 sm:mt-0">
          {allLabel} <span aria-hidden="true">→</span>
        </Link>
      </div>

      <ul className="mt-7 grid gap-6 md:grid-cols-2 lg:gap-8">
        {items.map((item) => (
          <li key={item.id}>
            <article className="group">
              <Link href={item.href} prefetch={false} className="focus-ring block">
                <div className={`relative aspect-[4/3] overflow-hidden border border-line/70 bg-surface-subtle sm:aspect-[16/10] ${compactMediaCorners ? "rounded-lg" : "rounded-xl"}`}>
                  {item.imageUrl ? (
                    <Image
                      src={item.imageUrl}
                      alt={item.imageAlt}
                      fill
                      loading="lazy"
                      className="object-cover transition-transform duration-300 motion-reduce:transition-none group-hover:scale-[1.018]"
                      sizes="(min-width: 768px) 48vw, calc(100vw - 2rem)"
                    />
                  ) : (
                    <div className="flex size-full flex-col items-center justify-center gap-3 px-6 text-center text-ink-muted">
                      <svg aria-hidden="true" viewBox="0 0 64 64" className="size-14 text-brand-600/40" fill="none" stroke="currentColor" strokeWidth="1.5">
                        <path d="M12 48 25 34l8 8 7-7 12 13" />
                        <path d="M10 14h44v36H10z" />
                        <circle cx="42" cy="25" r="4" />
                      </svg>
                      <span className="text-sm">Görsel yakında eklenecek</span>
                    </div>
                  )}
                </div>

                <div className="flex items-start justify-between gap-6 border-b border-line px-0.5 py-5">
                  <div className="min-w-0">
                    <h3 className="text-xl font-bold tracking-[-0.02em] text-ink transition-colors group-hover:text-brand-700 sm:text-2xl">
                      {item.name}
                    </h3>
                    <p className="mt-1.5 text-sm text-ink-muted">{item.productCount} ürün</p>
                  </div>
                  <span className="mt-0.5 flex size-10 shrink-0 items-center justify-center rounded-full border border-line text-brand-700 transition-colors group-hover:border-brand-700 group-hover:bg-brand-700 group-hover:text-white" aria-hidden="true">
                    →
                  </span>
                </div>
              </Link>
            </article>
          </li>
        ))}
      </ul>
    </section>
  );
}

// Burada Alt Banner 1'in ilk gerçek resmini metinle dengelenen bir mağaza hikâyesi alanına dönüştürüyorum.
export function BrandStory({ image }: { image?: BannerSectionItem }) {
  if (!image || image.mediaType !== 1 || !image.mediaUrl) return null;

  return (
    <section className="home-shell py-8 sm:py-10 lg:py-12" aria-labelledby="home-brand-story-title">
      <div className="overflow-hidden rounded-2xl border border-line bg-brand-950 lg:grid lg:grid-cols-[1.08fr_0.92fr]">
        <div className="relative aspect-[4/3] bg-surface-subtle lg:aspect-auto lg:min-h-[30rem]">
          <Image
            src={image.mediaUrl}
            alt={image.altText?.trim() || image.name}
            fill
            loading="lazy"
            className="object-cover"
            sizes="(min-width: 1024px) 54vw, calc(100vw - 2rem)"
          />
        </div>
        <div className="flex flex-col justify-center px-6 py-10 text-white sm:px-10 sm:py-14 lg:px-14">
          <p className="text-xs font-bold uppercase tracking-[0.2em] text-footer-icon">ELEVEN HİKÂYESİ</p>
          <h2 id="home-brand-story-title" className="mt-4 text-3xl font-semibold leading-tight tracking-[-0.03em] sm:text-4xl">
            Kalite, ayrıntılarda kendini gösterir.
          </h2>
          <p className="mt-5 text-base leading-7 text-footer-muted">
            ELEVEN’da her parça; malzeme hissi, tasarım dengesi ve günlük stile uyumu gözetilerek tasarlanır. Zamansız bir görünümü, özenli bir sunum ve güven veren bir alışveriş deneyimiyle buluşturuyoruz.
          </p>
          <p className="mt-4 text-sm leading-6 text-footer-muted">
            Çünkü bizim için iyi bir tasarım, yalnızca dikkat çekmekle kalmaz; kişisel tarzınıza doğal ve kalıcı bir ifade kazandırır.
          </p>
          <Link href="/products" className="focus-ring mt-8 inline-flex min-h-11 w-fit items-center gap-2 border-b border-footer-icon pb-1 text-sm font-bold text-white hover:border-white">
            Koleksiyonumuzu keşfedin <span aria-hidden="true">→</span>
          </Link>
        </div>
      </div>
    </section>
  );
}
