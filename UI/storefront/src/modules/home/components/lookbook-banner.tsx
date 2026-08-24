import Image from "next/image";
import Link from "next/link";

interface LookbookBannerProps {
  imageUrl?: string | null;
  title?: string;
  subtitle?: string;
  collectionHref?: string;
}

// Burada mağazanın editöryal stil atmosferini yansıtan lüks ve interaktif (Shoppable) lookbook banner bloğunu sunuyorum.
export function LookbookBanner({
  imageUrl = "https://res.cloudinary.com/zqnbecc5/image/upload/v1787215227/products/P0001B/ecn2o2pwu3eytyfuvilt.jpg",
  title = "Zamansız Zarafet: Sonbahar & Kış Koleksiyonu",
  subtitle = "Modern hatlar, usta işçilik ve heykelsi detaylar. Gündüzden geceye her anınıza eşlik edecek imza aksesuarlar.",
  collectionHref = "/collection/statement",
}: LookbookBannerProps) {
  return (
    <section aria-labelledby="lookbook-heading" className="home-shell py-8 sm:py-12">
      <div className="relative overflow-hidden rounded-3xl bg-brand-950 border border-line/80 text-white shadow-panel lg:grid lg:grid-cols-12 items-center">
        {/* Metin ve Çağrı Alanı */}
        <div className="relative z-10 flex flex-col justify-center px-6 py-10 sm:px-10 sm:py-14 lg:col-span-6 lg:p-14">
          <div className="inline-flex items-center gap-2 mb-3">
            <span className="h-px w-6 bg-footer-icon" />
            <span className="text-xs font-bold uppercase tracking-[0.25em] text-footer-icon">
              LOOKBOOK &bull; 2026 EDİT
            </span>
          </div>

          <h2 id="lookbook-heading" className="text-2xl font-bold tracking-tight text-white sm:text-4xl leading-tight">
            {title}
          </h2>
          <p className="mt-4 text-sm sm:text-base leading-relaxed text-footer-muted">
            {subtitle}
          </p>

          <div className="mt-8 flex flex-wrap items-center gap-4">
            <Link
              href={collectionHref}
              prefetch={false}
              className="focus-ring inline-flex items-center gap-2 rounded-xl bg-white px-6 py-3.5 text-sm font-bold text-brand-950 shadow-sm transition-all hover:bg-surface-subtle"
            >
              <span>Koleksiyonu Keşfet</span>
              <span aria-hidden="true">&rarr;</span>
            </Link>
            <Link
              href="/products"
              prefetch={false}
              className="focus-ring inline-flex items-center gap-2 rounded-xl border border-footer-line px-6 py-3.5 text-sm font-semibold text-white transition-all hover:bg-white/10"
            >
              Tüm Ürünler
            </Link>
          </div>
        </div>

        {/* Görseli mobilde kaynağın portre oranında tutarak taşmayı ve ürün etiketinin kırpılmasını önlüyorum. */}
        <div className="group relative aspect-[4/5] w-full overflow-hidden bg-surface-subtle/20 sm:aspect-[4/3] lg:col-span-6 lg:h-full lg:min-h-[30rem] lg:aspect-auto">
          {imageUrl ? (
            <Image
              src={imageUrl}
              alt="ELEVEN Sonbahar ve Kış Koleksiyonu"
              fill
              loading="lazy"
              className="object-cover object-center transition-transform duration-700 motion-safe:lg:hover:scale-105"
              sizes="(min-width: 1280px) 40rem, (min-width: 1024px) calc(50vw - 3rem), (min-width: 640px) calc(100vw - 3rem), calc(100vw - 2rem)"
            />
          ) : (
            <div className="flex size-full items-center justify-center bg-brand-950/60 text-footer-icon">
              ELEVEN LOOKBOOK
            </div>
          )}

          {/* Shoppable Hotspot Pin */}
          <Link
            href="/products/sculptural-torque-choker-altin"
            prefetch={false}
            className="focus-ring absolute bottom-4 left-1/2 z-20 flex max-w-[calc(100%_-_2rem)] -translate-x-1/2 items-center gap-3 rounded-xl border border-white/20 bg-brand-950/90 p-2.5 pr-4 shadow-lg transition-colors hover:bg-brand-950 sm:right-8 sm:bottom-8 sm:left-auto sm:translate-x-0"
            aria-label="Görseldeki ürün: Sculptural Torque Choker"
          >
            <div className="relative flex size-4 items-center justify-center">
              <span className="absolute inline-flex size-full animate-ping rounded-full bg-amber-400 opacity-75 motion-reduce:animate-none" />
              <span className="relative inline-flex size-2 rounded-full bg-amber-400" />
            </div>
            <div className="text-left">
              <p className="text-[0.6875rem] font-bold text-white leading-none">Sculptural Choker</p>
              <p className="text-[0.625rem] text-footer-muted leading-tight mt-0.5">1.299 TL &bull; İncele &rarr;</p>
            </div>
          </Link>

          <div className="absolute inset-0 bg-gradient-to-t from-brand-950/80 via-transparent to-transparent lg:hidden pointer-events-none" />
        </div>
      </div>
    </section>
  );
}
