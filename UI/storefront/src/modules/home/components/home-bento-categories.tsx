import Image from "next/image";
import Link from "next/link";
import type { CategoryShowcaseItem } from "@/modules/catalog/categories";

// Burada ana sayfa için klasik kategori listesi yerine asimetrik, modern ve lüks bir Bento Grid vitrini sunuyorum.
export function HomeBentoCategories({
  categories,
}: {
  categories: CategoryShowcaseItem[];
}) {
  if (!categories || categories.length === 0) return null;

  const [leadCategory, ...subCategories] = categories;
  const displaySubCategories = subCategories.slice(0, 4);

  return (
    <section
      id="home-categories-bento"
      aria-labelledby="bento-categories-heading"
      className="home-shell py-10 sm:py-14"
    >
      <div className="flex flex-col gap-2 sm:flex-row sm:items-end sm:justify-between border-b border-line pb-4 mb-8">
        <div>
          <p className="text-xs font-bold uppercase tracking-[0.2em] text-brand-700">
            KATEGORİLER
          </p>
          <h2
            id="bento-categories-heading"
            className="mt-1 text-2xl font-bold tracking-tight text-ink sm:text-3xl"
          >
            Tarzınızı Tamamlayan Kategoriler
          </h2>
          <p className="mt-1 text-sm text-ink-muted">
            En çok tercih edilen kategorileri keşfedin; günlük ve özel kombinlerinizi zenginleştirin.
          </p>
        </div>
        <Link
          href="/categories"
          prefetch={false}
          className="focus-ring text-xs sm:text-sm font-semibold text-brand-700 hover:text-brand-950 transition-colors shrink-0"
        >
          Tüm Kategoriler <span aria-hidden="true">&rarr;</span>
        </Link>
      </div>

      {/* Bento Grid: 1 Büyük Vurgulu Kart (Sol) + 4 Kompakt Kart (Sağ Izgara) */}
      <div className="grid grid-cols-1 gap-5 sm:gap-6 lg:grid-cols-12">
        {/* Büyük Sol Vurgulu Kart */}
        {leadCategory ? (
          <article className="lg:col-span-6 group relative">
            <Link
              href={leadCategory.href}
              prefetch={false}
              className="focus-ring block relative h-80 sm:h-96 lg:h-full min-h-[22rem] lg:min-h-[28rem] overflow-hidden rounded-3xl border border-line/70 bg-surface-subtle shadow-xs transition-all duration-500 group-hover:shadow-xl group-hover:border-brand-700/40"
            >
              {leadCategory.imageUrl ? (
                <Image
                  src={leadCategory.imageUrl}
                  alt={leadCategory.imageAlt || leadCategory.name}
                  fill
                  loading="lazy"
                  className="object-cover transition-transform duration-700 ease-out motion-reduce:transition-none group-hover:scale-105"
                  sizes="(min-width: 1024px) 50vw, 100vw"
                />
              ) : (
                <div className="flex size-full items-center justify-center bg-gradient-to-br from-brand-950 to-brand-700 text-white font-bold">
                  {leadCategory.name}
                </div>
              )}

              {/* Lüks Karartma & Degrade */}
              <div className="absolute inset-0 bg-gradient-to-t from-brand-950/90 via-brand-950/25 to-transparent transition-opacity duration-300 group-hover:opacity-95" />

              {/* Buzlu Cam Rozeti */}
              <div className="absolute top-4 left-4">
                <span className="inline-flex items-center rounded-xl bg-white/20 backdrop-blur-md border border-white/30 px-3 py-1 text-[0.6875rem] font-bold uppercase tracking-wider text-white shadow-xs">
                  Öne Çıkan Kategori
                </span>
              </div>

              {/* Alt Metin ve Aksiyon */}
              <div className="absolute bottom-0 left-0 right-0 p-6 sm:p-8 text-white">
                <p className="text-xs uppercase tracking-[0.15em] text-footer-icon font-semibold mb-1">
                  {leadCategory.productCount} Farklı Model
                </p>
                <h3 className="text-2xl sm:text-3xl font-bold tracking-tight text-white drop-shadow-sm">
                  {leadCategory.name}
                </h3>
                <span className="mt-4 inline-flex items-center gap-2 rounded-xl bg-white/20 backdrop-blur-md border border-white/30 px-4 py-2 text-xs font-bold text-white transition-all group-hover:bg-white group-hover:text-brand-950">
                  <span>Kategoriyi Keşfet</span>
                  <span aria-hidden="true">&rarr;</span>
                </span>
              </div>
            </Link>
          </article>
        ) : null}

        {/* Sağ 2x2 Kompakt Kartlar */}
        <div className="lg:col-span-6 grid grid-cols-2 gap-4 sm:gap-6">
          {displaySubCategories.map((cat) => (
            <article key={cat.id} className="group relative">
              <Link
                href={cat.href}
                prefetch={false}
                className="focus-ring block relative aspect-[4/5] sm:aspect-square lg:aspect-auto lg:h-[13.5rem] overflow-hidden rounded-3xl border border-line/70 bg-surface-subtle shadow-xs transition-all duration-500 group-hover:shadow-lg group-hover:border-brand-700/40"
              >
                {cat.imageUrl ? (
                  <Image
                    src={cat.imageUrl}
                    alt={cat.imageAlt || cat.name}
                    fill
                    loading="lazy"
                    className="object-cover transition-transform duration-700 ease-out motion-reduce:transition-none group-hover:scale-105"
                    sizes="(min-width: 1024px) 25vw, 50vw"
                  />
                ) : (
                  <div className="flex size-full items-center justify-center bg-gradient-to-br from-surface to-surface-subtle text-ink font-semibold">
                    {cat.name}
                  </div>
                )}

                <div className="absolute inset-0 bg-gradient-to-t from-black/80 via-black/15 to-transparent" />

                <div className="absolute bottom-0 left-0 right-0 p-4 text-white">
                  <h3 className="text-base sm:text-lg font-bold tracking-tight text-white drop-shadow-xs transition-colors group-hover:text-footer-icon">
                    {cat.name}
                  </h3>
                  <p className="text-[0.6875rem] text-white/80 mt-0.5">
                    {cat.productCount} Ürün &bull; İncele &rarr;
                  </p>
                </div>
              </Link>
            </article>
          ))}
        </div>
      </div>
    </section>
  );
}
