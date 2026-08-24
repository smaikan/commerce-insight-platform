import Image from "next/image";
import Link from "next/link";
import type { CollectionShowcaseItem } from "@/modules/catalog/collections";

// Burada ana sayfa için özel 3 sütunlu editoryal koleksiyon vitrinini dergi/lookbook estetiği ve filigran numaralandırmasıyla sunuyorum.
export function HomeCuratedCollections({
  collections,
}: {
  collections: CollectionShowcaseItem[];
}) {
  if (!collections || collections.length === 0) return null;

  // Yeni Sezon koleksiyonunu bul ve tam olarak "Yeni Sezon" rozetine (2. sıraya / 02) yerleştir
  const yeniSezon = collections.find(
    (c) => c.url === "yeni-sezon" || c.name.toLowerCase().includes("yeni sezon")
  );

  const others = collections.filter(
    (c) => c.id !== yeniSezon?.id
  );

  const displayCollections: CollectionShowcaseItem[] = [];
  if (others[0]) displayCollections.push(others[0]);
  if (yeniSezon) displayCollections.push(yeniSezon);
  else if (others[1]) displayCollections.push(others[1]);

  const remaining = others.find((c) => !displayCollections.some((d) => d.id === c.id));
  if (remaining && displayCollections.length < 3) {
    displayCollections.push(remaining);
  }

  const badgeLabels = ["İmza Koleksiyon", "Yeni Sezon", "Öne Çıkanlar"];
  const numerals = ["01", "02", "03"];

  return (
    <section
      id="home-curated-collections"
      aria-labelledby="curated-collections-heading"
      className="home-shell py-10 sm:py-14"
    >
      <div className="flex flex-col gap-2 sm:flex-row sm:items-end sm:justify-between border-b border-line pb-4 mb-8">
        <div>
          <p className="text-xs font-bold uppercase tracking-[0.2em] text-brand-700">
            ÖZEL KOLEKSİYONLAR
          </p>
          <h2
            id="curated-collections-heading"
            className="mt-1 text-2xl font-bold tracking-tight text-ink sm:text-3xl"
          >
            İlham Veren Tematik Koleksiyonlar
          </h2>
          <p className="mt-1 text-sm text-ink-muted">
            Farklı stilleri ve uyumlu parçaları bir arada sunan özel konsept koleksiyonlar.
          </p>
        </div>
        <Link
          href="/collections"
          prefetch={false}
          className="focus-ring text-xs sm:text-sm font-semibold text-brand-700 hover:text-brand-950 transition-colors shrink-0"
        >
          Tüm Koleksiyonlar <span aria-hidden="true">&rarr;</span>
        </Link>
      </div>

      <div className="grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-3 lg:gap-8">
        {displayCollections.map((col, idx) => (
          <article key={col.id} className="group relative flex flex-col">
            <Link
              href={col.href}
              prefetch={false}
              className="focus-ring block overflow-hidden rounded-3xl border border-line/70 bg-surface-subtle shadow-xs transition-all duration-500 group-hover:-translate-y-1.5 group-hover:shadow-xl group-hover:border-brand-700/40"
            >
              <div className="relative aspect-[4/5] w-full overflow-hidden">
                {col.imageUrl ? (
                  <Image
                    src={col.imageUrl}
                    alt={col.imageAlt || col.name}
                    fill
                    loading="lazy"
                    className="object-cover transition-transform duration-700 ease-out motion-reduce:transition-none group-hover:scale-105"
                    sizes="(min-width: 1024px) 33vw, (min-width: 640px) 50vw, 100vw"
                  />
                ) : (
                  <div className="flex size-full flex-col items-center justify-center gap-3 bg-gradient-to-br from-brand-950 to-brand-700 p-8 text-center text-white">
                    <svg
                      aria-hidden="true"
                      viewBox="0 0 48 48"
                      className="size-12 opacity-40"
                      fill="none"
                      stroke="currentColor"
                      strokeWidth="1.25"
                    >
                      <polygon points="24 4 44 24 24 44 4 24" />
                      <polygon points="24 12 36 24 24 36 12 24" />
                    </svg>
                    <span className="text-xs uppercase tracking-[0.2em] font-semibold text-footer-icon">
                      ELEVEN ATÖLYE
                    </span>
                  </div>
                )}

                {/* Arka Plan Numaralandırma Filigranı */}
                <div className="absolute top-2 right-4 text-white/15 text-7xl sm:text-8xl font-black select-none pointer-events-none transition-transform duration-500 group-hover:scale-110 group-hover:text-white/20">
                  {numerals[idx % numerals.length]}
                </div>

                {/* Degrade Katmanı */}
                <div className="absolute inset-0 bg-gradient-to-t from-brand-950/90 via-brand-950/20 to-transparent transition-opacity duration-300 group-hover:opacity-95" />

                {/* Buzlu Cam Rozeti */}
                <div className="absolute top-4 left-4">
                  <span className="inline-flex items-center rounded-xl bg-brand-950/65 backdrop-blur-md border border-white/20 px-3 py-1 text-[0.6875rem] font-bold uppercase tracking-wider text-white shadow-xs">
                    {badgeLabels[idx % badgeLabels.length]}
                  </span>
                </div>

                {/* Kart İçi Başlık ve Bilgi */}
                <div className="absolute bottom-0 left-0 right-0 p-6 sm:p-7 text-white">
                  <p className="text-xs uppercase tracking-[0.15em] text-footer-icon font-semibold mb-1">
                    {col.productCount > 0 ? `${col.productCount} Özel Parça` : "Yeni Tasarımlar"}
                  </p>
                  <h3 className="text-xl sm:text-2xl font-bold tracking-tight text-white drop-shadow-xs">
                    {col.name}
                  </h3>
                  <div className="mt-3 inline-flex items-center gap-2 text-xs font-bold text-white transition-transform duration-300 group-hover:translate-x-1">
                    <span>Koleksiyonu Keşfet</span>
                    <span aria-hidden="true">&rarr;</span>
                  </div>
                </div>
              </div>
            </Link>
          </article>
        ))}
      </div>
    </section>
  );
}
