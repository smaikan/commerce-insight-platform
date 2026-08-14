import Image from "next/image";
import Link from "next/link";

import { formatCurrency } from "@/lib/formatting/currency";
import { FavoriteButton } from "@/modules/favorites/components/favorite-button";
import type { FavoriteProduct } from "@/modules/favorites/types";

// Burada favori DTO'sunda gerçekten bulunan aktif varyantlardan yalnız sunum amaçlı en düşük fiyatı seçiyorum.
function favoriteProductPrice(product: FavoriteProduct): number | null {
  // Burada eksik varyant dizisinin hesap ekranını düşürmesini engelliyorum.
  const variants = Array.isArray(product.variants) ? product.variants : [];
  const prices = variants.filter((variant) => variant.isActive).map((variant) => variant.price);
  return prices.length > 0 ? Math.min(...prices) : null;
}

// Burada favori ürünü mevcut API alanlarıyla, eksik medya/fiyat ve satıştan kalkma durumlarını dürüstçe göstererek sunuyorum.
export function FavoriteProductCard({ product }: { product: FavoriteProduct }) {
  const href = `/products/${encodeURIComponent(product.url)}`;
  const isPublished = product.isActive && product.status === 1;
  const price = favoriteProductPrice(product);
  // Burada eksik görsel dizisini kararlı boş-görsel durumuna indiriyorum.
  const images = Array.isArray(product.images) ? product.images : [];
  const mainImage = product.mainImage?.imageUrl.trim() ? product.mainImage : null;
  const image = mainImage || images.find((candidate) => candidate.imageUrl.trim().length > 0) || null;

  return (
    <article className="group relative min-w-0">
      <FavoriteButton
        productId={product.id}
        productTitle={product.title}
        initiallyFavorite
      />
      {isPublished ? (
        <Link href={href} prefetch={false} className="focus-ring block">
          <FavoriteProductContent product={product} image={image} price={price} />
        </Link>
      ) : (
        <div>
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
      <div className="relative aspect-[4/5] overflow-hidden rounded-xl border border-line/70 bg-surface-subtle">
        {image ? (
          <Image
            src={image.imageUrl}
            alt={image.altText || product.title}
            fill
            loading="lazy"
            className="object-cover transition-transform duration-300 group-hover:scale-[1.015]"
            sizes="(min-width: 1280px) 17rem, (min-width: 1024px) 25vw, (min-width: 640px) 33vw, 46vw"
          />
        ) : (
          <div className="flex size-full items-center justify-center px-5 text-center text-sm text-ink-muted">
            Ürün görseli bulunmuyor
          </div>
        )}
      </div>
      <div className="px-0.5 pt-3.5">
        {product.brandName ? <p className="mb-1 truncate text-xs font-medium tracking-[0.06em] text-brand-700">{product.brandName}</p> : null}
        <h2 className="line-clamp-2 text-sm font-semibold leading-5 text-ink transition-colors group-hover:text-brand-700 sm:text-[0.9375rem]">{product.title}</h2>
        {unavailable ? (
          <p className="mt-2 text-sm font-semibold text-danger">Artık satışta değil</p>
        ) : price !== null ? (
          <p className="mt-2 text-[0.9375rem] font-bold text-ink sm:text-base">{formatCurrency(price)}</p>
        ) : (
          <p className="mt-2 text-sm text-ink-muted">Fiyat bilgisi mevcut değil</p>
        )}
      </div>
    </>
  );
}
