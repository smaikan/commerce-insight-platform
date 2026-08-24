import Image from "next/image";
import Link from "next/link";

import type { CategoryShowcaseItem, CategoryShowcasePage } from "@/modules/catalog/categories";
import { categoriesHref } from "@/modules/catalog/categories-query";

// Burada kategorileri koleksiyon sayfasıyla aynı lüks kart geometrisi ve editoryal görsel dilde erişilebilir bir vitrinde sunuyorum.
export function CategoryShowcase({ page }: { page: CategoryShowcasePage }) {
  const categories = page.items;

  return (
    <main id="main-content" className="flex-1 pb-16">
      {/* Breadcrumb Gezintisi & Başlık Alanı */}
      <header className="border-b border-line/70 bg-surface">
        <div className="page-shell max-w-[84rem] py-8 sm:py-10">
          <nav aria-label="Sayfa yolu" className="mb-3 flex items-center gap-1.5 text-xs text-ink-muted">
            <Link href="/" className="focus-ring hover:text-brand-700 transition-colors">
              Ana Sayfa
            </Link>
            <span aria-hidden="true" className="text-line">/</span>
            <span aria-current="page" className="font-semibold text-ink">
              Kategoriler
            </span>
          </nav>

          <div className="flex flex-col gap-2 sm:flex-row sm:items-end sm:justify-between">
            <div>
              <p className="text-xs font-bold uppercase tracking-[0.2em] text-brand-700">
                ELEVEN KATALOG
              </p>
              <h1 className="mt-1 text-3xl font-bold tracking-tight text-ink sm:text-4xl lg:text-5xl">
                Kategoriler
              </h1>
              <p className="mt-2 max-w-xl text-xs sm:text-sm text-ink-muted leading-relaxed">
                Tüm takı ve aksesuar koleksiyonlarını kategori bazında keşfedin.
              </p>
            </div>
            <p className="text-xs sm:text-sm font-semibold text-brand-950">
              {page.totalCount} kategori
            </p>
          </div>
        </div>
      </header>

      <section className="page-shell max-w-[84rem] pt-8 sm:pt-10" aria-labelledby="category-list-title">
        <h2 id="category-list-title" className="sr-only">Tüm kategoriler</h2>

        {categories.length > 0 ? (
          <ul className="grid grid-cols-1 gap-x-6 gap-y-10 sm:grid-cols-2 md:grid-cols-3 md:gap-y-12">
            {categories.map((category, index) => (
              <CategoryCard
                key={category.id}
                category={category}
                isLcpCandidate={index < 3}
              />
            ))}
          </ul>
        ) : (
          <div className="rounded-2xl border border-line/80 bg-surface p-12 text-center shadow-xs">
            <h2 className="text-xl font-bold text-ink">Henüz görüntülenecek kategori yok</h2>
            <p className="mx-auto mt-2 max-w-lg text-sm leading-relaxed text-ink-muted">
              Yayınlanmış ürünlere sahip kategoriler hazır olduğunda burada görünecek.
            </p>
            <Link href="/products" className="focus-ring mt-6 inline-flex items-center gap-2 rounded-xl bg-brand-950 px-6 py-3 text-xs font-bold text-white hover:bg-brand-700 transition-colors">
              <span>Kataloğa Git</span>
              <span aria-hidden="true">&rarr;</span>
            </Link>
          </div>
        )}

        <CategoryPagination page={page} />
      </section>
    </main>
  );
}

// Burada API sayfalamasını erişilebilir önceki ve sonraki bağlantılarıyla sunuyorum.
function CategoryPagination({ page }: { page: CategoryShowcasePage }) {
  if (page.totalPages <= 1) return null;

  return (
    <nav className="mt-14 flex flex-wrap items-center justify-between gap-4 border-t border-line/70 pt-8" aria-label="Kategori sayfaları">
      <div className="flex items-center">
        {page.hasPreviousPage ? (
          <Link
            className="focus-ring inline-flex items-center gap-2 rounded-xl border border-line bg-surface px-4 py-2.5 text-xs font-bold text-ink hover:border-brand-700 hover:text-brand-950 transition-all shadow-2xs"
            href={categoriesHref({ page: page.pageNumber - 1, pageSize: page.pageSize })}
            rel="prev"
          >
            <span aria-hidden="true">&larr;</span>
            <span>Önceki</span>
          </Link>
        ) : <span />}
      </div>

      <p className="text-xs sm:text-sm text-ink-muted">
        Sayfa <span className="font-bold text-ink">{page.pageNumber}</span> / <span className="font-bold text-ink">{page.totalPages}</span>
      </p>

      <div className="flex items-center">
        {page.hasNextPage ? (
          <Link
            className="focus-ring inline-flex items-center gap-2 rounded-xl border border-line bg-surface px-4 py-2.5 text-xs font-bold text-ink hover:border-brand-700 hover:text-brand-950 transition-all shadow-2xs"
            href={categoriesHref({ page: page.pageNumber + 1, pageSize: page.pageSize })}
            rel="next"
          >
            <span>Sonraki</span>
            <span aria-hidden="true">&rarr;</span>
          </Link>
        ) : <span />}
      </div>
    </nav>
  );
}

// Burada kategori kartını lüks editoryal hover efektleri ve şık rozetlerle sunuyorum.
function CategoryCard({
  category,
  isLcpCandidate,
}: {
  category: CategoryShowcaseItem;
  isLcpCandidate: boolean;
}) {
  return (
    <li>
      <article className="group">
        <Link href={category.href} prefetch={false} className="focus-ring block">
          <div className="relative aspect-[16/10] sm:aspect-[3/2] overflow-hidden rounded-lg border border-line/70 bg-surface-subtle shadow-xs transition-all duration-500 group-hover:shadow-lg group-hover:border-brand-700/40">
            {category.imageUrl ? (
              <Image
                src={category.imageUrl}
                alt={category.imageAlt}
                fill
                loading={isLcpCandidate ? "eager" : "lazy"}
                fetchPriority={isLcpCandidate ? "high" : undefined}
                className="object-cover transition-transform duration-700 ease-out motion-reduce:transition-none group-hover:scale-105"
                sizes="(min-width: 1280px) 30vw, (min-width: 768px) 31vw, (min-width: 640px) 48vw, calc(100vw - 2rem)"
              />
            ) : (
              <div className="flex size-full flex-col items-center justify-center gap-2 bg-gradient-to-br from-surface to-surface-subtle px-6 text-center text-brand-700">
                <svg aria-hidden="true" viewBox="0 0 48 48" className="size-10 text-brand-600/45 transition-transform duration-500 group-hover:scale-110" fill="none" stroke="currentColor" strokeWidth="1.25">
                  <path d="M24 6l14 10-14 26L10 16z" />
                </svg>
                <span className="text-[0.6875rem] font-bold uppercase tracking-widest text-ink-muted/80">Kategori görseli bulunmuyor</span>
              </div>
            )}

            <div className="absolute inset-0 bg-gradient-to-t from-black/60 via-transparent to-transparent opacity-0 transition-opacity duration-300 group-hover:opacity-100" />
          </div>

          <div className="flex items-start justify-between gap-4 border-b border-line/60 px-0.5 py-4 transition-colors group-hover:border-brand-700/40">
            <div className="min-w-0">
              <h2 className="text-lg font-bold tracking-tight text-ink transition-colors group-hover:text-brand-700 sm:text-xl">
                {category.name}
              </h2>
              <p className="mt-1 text-xs font-semibold text-brand-700">{category.productCount} ürün</p>
            </div>
            <span
              className="mt-0.5 flex size-9 shrink-0 items-center justify-center rounded-xl border border-line bg-surface text-brand-700 shadow-2xs transition-all duration-300 group-hover:border-brand-950 group-hover:bg-brand-950 group-hover:text-white group-hover:translate-x-0.5"
              aria-hidden="true"
            >
              &rarr;
            </span>
          </div>
        </Link>
      </article>
    </li>
  );
}
