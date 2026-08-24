import Image from "next/image";
import Link from "next/link";

import { formatCurrency } from "@/lib/formatting/currency";
import { FavoriteButton } from "@/modules/favorites/components/favorite-button";
import type { FavoriteProduct } from "@/modules/favorites/types";

// Burada favori DTO'sunda gerçekten bulunan aktif varyantlardan yalnız sunum amaçlı en düşük fiyatı seçiyorum.
function favoriteProductPrice(product: FavoriteProduct): number | null {
  const variants = Array.isArray(product.variants) ? product.variants : [];
  const prices = variants.filter((variant) => variant.isActive).map((variant) => variant.price);
  return prices.length > 0 ? Math.min(...prices) : null;
}

// Burada favori ürünü mobilde derli toplu lüks kart çerçevesi, dengeli tipografi ve 4:5 medya oranıyla sunuyorum.
export function FavoriteProductCard({ product }: { product: FavoriteProduct }) {
  const href = `/products/${encodeURIComponent(product.url)}`;
  const isPublished = product.isActive && product.status === 1;
  const price = favoriteProductPrice(product);
  const images = Array.isArray(product.images) ? product.images : [];
  const mainImage = product.mainImage?.imageUrl.trim() ? product.mainImage : null;
  const image = mainImage || images.find((candidate) => candidate.imageUrl.trim().length > 0) || null;

  return (
    <article className="group relative flex h-full flex-col min-w-0 rounded-2xl border border-line/60 bg-surface p-2 sm:p-3 shadow-xs transition-all duration-300 hover:border-brand-700/30 hover:shadow-lg">
      <FavoriteButton
        productId={product.id}
        productTitle={product.title}
        initiallyFavorite
      />
      {isPublished ? (
        <Link href={href} prefetch={false} className="focus-ring flex flex-1 flex-col">
          <FavoriteProductContent product={product} image={image} price={price} />
        </Link>
      ) : (
        <div className="flex flex-1 flex-col">
          <FavoriteProductContent product={product} image={image} price={price} unavailable />
        </div>
      )}
    </article>
  );
}

// Burada linkli ve satıştan kalkmış kartların aynı 4:5 geometrisini tek sunum bloğunda koruyorum.
function FavoriteProductContent({
  product,
  image,
  price,
  unavailable = false,
}: {
  product: FavoriteProduct;
  image: FavoriteProduct["mainImage"] | null;
  price: number | null;
  unavailable?: boolean;
}) {
  return (
    <>
      <div className="relative aspect-[4/5] w-full overflow-hidden rounded-xl bg-surface-subtle">
        {image ? (
          <Image
            src={image.imageUrl}
            alt={image.altText || product.title}
            fill
            loading="lazy"
            className="object-cover transition-transform duration-700 ease-out group-hover:scale-105"
            sizes="(min-width: 1280px) 17rem, (min-width: 1024px) 25vw, (min-width: 640px) 33vw, 46vw"
          />
        ) : (
          <div className="flex size-full items-center justify-center px-4 text-center text-xs text-ink-muted">
            Ürün görseli bulunmuyor
          </div>
        )}
      </div>
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
        <div className="mt-2">
          {unavailable ? (
            <p className="text-xs sm:text-sm font-semibold text-danger">Artık satışta değil</p>
          ) : price !== null ? (
            <p className="text-sm sm:text-base font-bold tracking-tight text-brand-950">{formatCurrency(price)}</p>
          ) : (
            <p className="text-xs sm:text-sm text-ink-muted">Fiyat bilgisi mevcut değil</p>
          )}
        </div>
      </div>
    </>
  );
}
