import Image from "next/image";
import Link from "next/link";

import { formatCurrency } from "@/lib/formatting/currency";
import { FavoriteButton } from "@/modules/favorites/components/favorite-button";
import type { PublishedProduct } from "@/modules/catalog/types";

// Burada ürün kartını mobilde derli toplu lüks kart çerçevesi, dengeli tipografi, 4:5 medya oranı ve masaüstü slide-up aksiyonuyla sunuyorum.
export function ProductCard({
  product,
  isLcpCandidate = false,
}: {
  product: PublishedProduct;
  isLcpCandidate?: boolean;
}) {
  const hasDiscount =
    product.price !== null &&
    product.price !== undefined &&
    product.compareAtPrice !== null &&
    product.compareAtPrice !== undefined &&
    product.compareAtPrice > product.price;

  const discountPercentage = hasDiscount
    ? Math.round(((product.compareAtPrice! - product.price!) / product.compareAtPrice!) * 100)
    : null;

  const isOutOfStock = product.isAvailable === false;
  const isLowStock = !isOutOfStock && product.isLowStock && product.lowestAvailableStock && product.lowestAvailableStock > 0;
  const href = `/products/${encodeURIComponent(product.url)}`;

  return (
    <article className="group relative flex h-full flex-col min-w-0 rounded-2xl border border-line/60 bg-surface p-2 sm:p-3 shadow-xs transition-all duration-300 hover:border-brand-700/30 hover:shadow-lg">
      <FavoriteButton productId={product.id} productTitle={product.title} />

      <Link className="focus-ring flex flex-1 flex-col" href={href} prefetch={false}>
        {/* Görsel Alanı */}
        <div className="relative aspect-[4/5] w-full overflow-hidden rounded-xl bg-surface-subtle">
          {product.mainImage ? (
            <Image
              src={product.mainImage.imageUrl}
              alt={product.mainImage.altText || product.title}
              fill
              loading={isLcpCandidate ? "eager" : "lazy"}
              fetchPriority={isLcpCandidate ? "high" : undefined}
              className={`object-cover transition-transform duration-700 ease-out motion-reduce:transition-none group-hover:scale-105 ${
                isOutOfStock ? "opacity-60 grayscale-[40%]" : ""
              }`}
              sizes="(min-width: 1280px) 19rem, (min-width: 1024px) 24vw, (min-width: 768px) 31vw, 46vw"
            />
          ) : (
            <div className="flex size-full flex-col items-center justify-center gap-2.5 bg-gradient-to-b from-surface-subtle to-line/30 px-4 text-center">
              <svg
                aria-hidden="true"
                viewBox="0 0 48 48"
                className="size-10 text-brand-600/40 transition-transform duration-500 group-hover:scale-110"
                fill="none"
                stroke="currentColor"
                strokeWidth="1.25"
                strokeLinecap="round"
                strokeLinejoin="round"
              >
                <path d="M24 6l14 10-14 26L10 16z" />
                <path d="M10 16h28M17 16l7 26 7-26M24 6v10M17 16l-7-10 14 10 14-10-7 10" />
              </svg>
              <span className="text-[10px] font-semibold tracking-widest text-ink-muted/80 uppercase">ELEVEN</span>
            </div>
          )}

          {/* Lüks Rozetler */}
          <div className="absolute left-2 top-2 sm:left-2.5 sm:top-2.5 flex flex-col gap-1 pointer-events-none z-10">
            {hasDiscount && discountPercentage ? (
              <span className="inline-flex items-center rounded-full bg-[#8B1E2D] px-2 py-0.5 text-[0.625rem] sm:text-xs font-bold tracking-wider text-white shadow-xs">
                -%{discountPercentage}
              </span>
            ) : null}

            {isOutOfStock ? (
              <span className="inline-flex items-center rounded-full bg-ink-muted/90 px-2 py-0.5 text-[0.625rem] sm:text-xs font-semibold text-white shadow-xs backdrop-blur-xs">
                Tükendi
              </span>
            ) : isLowStock ? (
              <span className="inline-flex items-center rounded-full bg-brand-950/90 px-2 py-0.5 text-[0.625rem] sm:text-xs font-medium text-white shadow-xs backdrop-blur-xs">
                Son {product.lowestAvailableStock}
              </span>
            ) : null}
          </div>

          {/* Masaüstü Slide-Up "Hızlı İncele" Barı */}
          <div className="absolute inset-x-3 bottom-3 hidden lg:flex items-center justify-center rounded-xl bg-surface/95 py-2 px-3 text-xs font-bold text-brand-950 shadow-md backdrop-blur-md transition-all duration-300 opacity-0 translate-y-3 group-hover:translate-y-0 group-hover:opacity-100 pointer-events-none">
            <span>Ürünü İncele</span>
            <span aria-hidden="true" className="ml-1.5 transition-transform group-hover:translate-x-0.5">&rarr;</span>
          </div>
        </div>

        {/* Metin ve Fiyat Alanı */}
        <div className="flex flex-1 flex-col justify-between pt-2.5 px-0.5 sm:pt-3">
          <div>
            {product.brandName ? (
              <p className="mb-0.5 truncate text-[10px] sm:text-xs font-bold uppercase tracking-[0.12em] text-brand-700">
                {product.brandName}
              </p>
            ) : null}
            <h2 className="line-clamp-2 min-h-[2rem] sm:min-h-[2.35rem] text-xs sm:text-[0.9375rem] font-medium leading-snug text-ink transition-colors group-hover:text-brand-700">
              {product.title}
            </h2>
          </div>

          <div className="mt-2 flex items-baseline flex-wrap gap-x-2 gap-y-0.5">
            {product.price !== null && product.price !== undefined ? (
              <span className="text-sm sm:text-base font-bold tracking-tight text-brand-950">
                {formatCurrency(product.price)}
              </span>
            ) : (
              <span className="text-xs sm:text-sm text-ink-muted">Fiyat bilgisi yok</span>
            )}
            {hasDiscount ? (
              <span className="text-[11px] sm:text-xs font-normal text-ink-muted/75 line-through">
                {formatCurrency(product.compareAtPrice!)}
              </span>
            ) : null}
          </div>
        </div>
      </Link>
    </article>
  );
}
