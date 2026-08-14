import Image from "next/image";
import Link from "next/link";

import type { CollectionShowcaseItem, CollectionShowcasePage } from "@/modules/catalog/collections";
import { collectionsHref } from "@/modules/catalog/collections-query";

// Burada ürün kataloğundan ayrışan editoryal mozaikle koleksiyonları görsel ve isim odağında sunuyorum.
export function CollectionShowcase({ page }: { page: CollectionShowcasePage }) {
  const collections = page.items;

  return (
    <main id="main-content" className="flex-1 pb-16 sm:pb-20">
      <header className="border-b border-line/80 bg-surface">
        <div className="page-shell py-9 sm:py-11 lg:flex lg:items-end lg:justify-between lg:gap-12 lg:py-12">
          <h1 className="text-3xl font-semibold tracking-[-0.035em] text-brand-950 sm:text-4xl">
            Koleksiyonlar
          </h1>
          {/* Burada tanıtım sloganı yerine API'nin authoritative toplam koleksiyon sayısını gösteriyorum. */}
          <p className="mt-3 text-sm font-medium text-ink-muted lg:mt-0">
            {page.totalCount} koleksiyon
          </p>
        </div>
      </header>

      <section className="page-shell pt-8 sm:pt-10 lg:pt-12" aria-labelledby="collection-list-title">
        <h2 id="collection-list-title" className="sr-only">Tüm koleksiyonlar</h2>

        {collections.length > 0 ? (
          <ul className="grid grid-cols-1 gap-x-5 gap-y-9 sm:grid-cols-2 sm:gap-y-11 xl:grid-cols-3 xl:gap-x-6 xl:gap-y-14">
            {collections.map((collection, index) => (
              <CollectionCard
                key={collection.id}
                collection={collection}
                featuredWidth={collection.isFeatured}
                isLcpCandidate={index === 0}
              />
            ))}
          </ul>
        ) : (
          <div className="border-y border-line py-14 text-center sm:py-20">
            <h2 className="text-xl font-bold text-brand-950">Henüz görüntülenecek koleksiyon yok</h2>
            <p className="mx-auto mt-2 max-w-lg text-sm leading-6 text-ink-muted">
              Yayımlanmış ürünlere sahip koleksiyonlar hazır olduğunda burada görünecek.
            </p>
            <Link href="/products" className="focus-ring mt-6 inline-flex min-h-11 items-center font-bold text-brand-700 hover:text-brand-950">
              Kataloğa git <span className="ml-2" aria-hidden="true">→</span>
            </Link>
          </div>
        )}

        <CollectionPagination page={page} />
      </section>
    </main>
  );
}

// Burada API sayfalama sonucunu erişilebilir önceki/sonraki bağlantılarıyla ve seçilmiş pageSize değeri korunarak sunuyorum.
function CollectionPagination({ page }: { page: CollectionShowcasePage }) {
  if (page.totalPages <= 1) return null;

  return (
    <nav className="mt-12 flex items-center justify-between border-t border-line pt-6" aria-label="Koleksiyon sayfaları">
      {page.hasPreviousPage ? (
        <Link
          className="focus-ring rounded-lg border border-line bg-surface px-4 py-2.5 text-sm font-semibold hover:border-brand-600"
          href={collectionsHref({ page: page.pageNumber - 1, pageSize: page.pageSize })}
          rel="prev"
        >
          Önceki
        </Link>
      ) : <span />}
      <p className="text-sm text-ink-muted"><span className="font-semibold text-ink">{page.pageNumber}</span> / {page.totalPages}</p>
      {page.hasNextPage ? (
        <Link
          className="focus-ring rounded-lg border border-line bg-surface px-4 py-2.5 text-sm font-semibold hover:border-brand-600"
          href={collectionsHref({ page: page.pageNumber + 1, pageSize: page.pageSize })}
          rel="next"
        >
          Sonraki
        </Link>
      ) : <span />}
    </nav>
  );
}

// Burada koleksiyon kartında değişken masaüstü genişliği kullanırken mobilde tek ve dengeli bir okuma akışı koruyorum.
function CollectionCard({
  collection,
  featuredWidth,
  isLcpCandidate,
}: {
  collection: CollectionShowcaseItem;
  featuredWidth: boolean;
  isLcpCandidate: boolean;
}) {
  const itemClassName = featuredWidth ? "xl:col-span-2" : "xl:col-span-1";
  const mediaClassName = featuredWidth
    ? "aspect-[16/10] sm:aspect-[3/2] xl:aspect-[3/1]"
    : "aspect-[16/10] sm:aspect-[3/2]";

  return (
    <li className={itemClassName}>
      <article className="group">
        <Link href={collection.href} prefetch={false} className="focus-ring block">
          <div className={`relative overflow-hidden rounded-xl border border-line/70 bg-surface-subtle ${mediaClassName}`}>
            {collection.imageUrl ? (
              <Image
                src={collection.imageUrl}
                alt={collection.imageAlt}
                fill
                loading={isLcpCandidate ? "eager" : "lazy"}
                fetchPriority={isLcpCandidate ? "high" : undefined}
                className="object-cover transition-transform duration-300 group-hover:scale-[1.018]"
                sizes={featuredWidth
                  ? "(min-width: 1280px) 62vw, (min-width: 640px) 48vw, calc(100vw - 2rem)"
                  : "(min-width: 1280px) 30vw, (min-width: 640px) 48vw, calc(100vw - 2rem)"}
              />
            ) : (
              <div className="flex size-full items-center justify-center bg-surface-subtle px-6 text-center">
                <svg aria-hidden="true" viewBox="0 0 64 64" className="size-14 text-brand-600/45" fill="none" stroke="currentColor" strokeWidth="1.5">
                  <path d="M12 48 25 34l8 8 7-7 12 13" />
                  <path d="M10 14h44v36H10z" />
                  <circle cx="42" cy="25" r="4" />
                </svg>
                <span className="sr-only">Koleksiyon görseli bulunmuyor</span>
              </div>
            )}
          </div>

          <div className="flex items-start justify-between gap-5 border-b border-line px-0.5 py-4 sm:py-5">
            <div className="min-w-0">
              <h2 className="text-lg font-bold tracking-[-0.015em] text-ink transition-colors group-hover:text-brand-700 sm:text-xl">
                {collection.name}
              </h2>
              <p className="mt-1 text-xs font-medium text-ink-muted">{collection.productCount} ürün</p>
            </div>
            <span className="mt-0.5 flex size-9 shrink-0 items-center justify-center rounded-full border border-line text-brand-700 transition-colors group-hover:border-brand-700 group-hover:bg-brand-700 group-hover:text-white" aria-hidden="true">
              →
            </span>
          </div>
        </Link>
      </article>
    </li>
  );
}
