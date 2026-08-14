import Image from "next/image";
import Link from "next/link";

import { formatCurrency } from "@/lib/formatting/currency";
import type { PublishedProduct } from "@/modules/catalog/types";

const ratingFormatter = new Intl.NumberFormat("tr-TR", { minimumFractionDigits: 1, maximumFractionDigits: 1 });

// Burada ürün kartını 4:5 medya oranı, gerçek API alanları ve yalnız ilk kart için öncelikli görsel yükleme desteğiyle sunuyorum.
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
  const href = `/products/${encodeURIComponent(product.url)}`;
  const formattedRating = ratingFormatter.format(product.averageRating);

  return (
    <article className="group min-w-0">
      <Link className="focus-ring block" href={href}>
        <div className="relative aspect-[4/5] overflow-hidden rounded-xl border border-line/70 bg-surface-subtle">
          {product.mainImage ? (
            <Image
              src={product.mainImage.imageUrl}
              alt={product.mainImage.altText || product.title}
              fill
              className="object-cover transition-transform duration-300 group-hover:scale-[1.015]"
              sizes="(min-width: 1280px) 19rem, (min-width: 1024px) 24vw, (min-width: 768px) 31vw, 46vw"
              loading={isLcpCandidate ? "eager" : "lazy"}
              fetchPriority={isLcpCandidate ? "high" : undefined}
            />
          ) : (
            <div className="flex size-full items-center justify-center px-5 text-center text-sm text-ink-muted">
              Ürün görseli bulunmuyor
            </div>
          )}
        </div>

        <div className="px-0.5 pt-3.5">
          {product.brandName ? (
            <p className="mb-1 truncate text-xs font-medium tracking-[0.06em] text-brand-700">
              {product.brandName}
            </p>
          ) : null}
          <h2 className="line-clamp-2 text-sm font-semibold leading-5 text-ink transition-colors group-hover:text-brand-700 sm:text-[0.9375rem]">
            {product.title}
          </h2>

          <div className="mt-2">
            <div className="flex flex-wrap items-baseline gap-x-2 gap-y-1">
              {product.price !== null && product.price !== undefined ? (
                <span className="text-[0.9375rem] font-bold tracking-[-0.01em] text-ink sm:text-base">
                  {formatCurrency(product.price)}
                </span>
              ) : (
                <span className="text-sm text-ink-muted">Fiyat bilgisi yok</span>
              )}
              {hasDiscount ? (
                <span className="text-xs text-ink-muted line-through">
                  {formatCurrency(product.compareAtPrice!)}
                </span>
              ) : null}
            </div>

            <div className="mt-1.5 min-h-5">
              {product.ratingCount > 0 ? (
                <p className="flex items-center gap-1.5 text-xs" aria-label={`${formattedRating} puan, ${product.ratingCount} değerlendirme`}>
                  <span className="text-brand-700" aria-hidden="true">★</span>
                  <span className="font-semibold text-ink">{formattedRating}</span>
                  <span className="text-ink-muted">({product.ratingCount})</span>
                </p>
              ) : null}
            </div>
          </div>
        </div>
      </Link>
    </article>
  );
}
