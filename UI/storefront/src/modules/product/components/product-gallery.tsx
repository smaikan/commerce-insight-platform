import Image from "next/image";

import { ProductCarouselControls } from "@/modules/product/components/product-carousel-controls";
import type { ProductImage } from "@/modules/product/types";

const CAROUSEL_ID = "product-media-carousel";

// Burada API görsellerini ana görsel ve yönetim sırasına göre tek otoriter düzende hazırlıyorum.
export function orderProductImages(images: ProductImage[]): ProductImage[] {
  return [...images].sort(
    (left, right) => Number(right.isMain) - Number(left.isMain) || left.displayOrder - right.displayOrder,
  );
}

// Burada tek görsel ağacını mobilde scroll-snap carousel, masaüstünde hizalı galeri olarak yeniden düzenliyorum.
export function ProductGallery({ images, productTitle }: { images: ProductImage[]; productTitle: string }) {
  if (images.length === 0) {
    return (
      <div className="min-w-0 lg:col-start-1 lg:row-start-1">
        <div className="mx-auto flex aspect-[4/5] w-full max-w-[34rem] items-center justify-center rounded-2xl border border-line bg-surface-subtle px-8 text-center text-sm text-ink-muted">
          Bu ürün için henüz görsel bulunmuyor
        </div>
      </div>
    );
  }

  const [primaryImage, ...additionalImages] = images;

  return (
    <div className="min-w-0 lg:contents">
      <div
        id={CAROUSEL_ID}
        role="region"
        aria-roledescription="carousel"
        aria-label="Ürün görselleri"
        className="product-media-carousel -mx-4 flex w-[calc(100%+2rem)] snap-x snap-mandatory overflow-x-auto overscroll-x-contain sm:mx-0 sm:w-auto lg:contents"
      >
        <div
          role="group"
          aria-roledescription="slide"
          aria-label={`1 / ${images.length}`}
          data-carousel-slide
          className="w-full min-w-0 flex-none snap-start lg:col-start-1 lg:row-start-1 lg:h-full"
        >
          <div className="lg:sticky lg:top-28">
            <ProductImageFrame image={primaryImage} alt={primaryImage.altText || `${productTitle} ana görseli`} primary />
          </div>
        </div>

        {additionalImages.length > 0 ? (
          <div className="contents lg:col-start-1 lg:row-start-2 lg:mx-auto lg:grid lg:w-full lg:max-w-[34rem] lg:grid-cols-2 lg:gap-4">
            {additionalImages.map((image, index) => (
              <div
                key={image.id}
                role="group"
                aria-roledescription="slide"
                aria-label={`${index + 2} / ${images.length}`}
                data-carousel-slide
                className="w-full min-w-0 flex-none snap-start"
              >
                <ProductImageFrame image={image} alt={image.altText || `${productTitle} görsel ${index + 2}`} />
              </div>
            ))}
          </div>
        ) : null}
      </div>

      <ProductCarouselControls carouselId={CAROUSEL_ID} count={images.length} />
    </div>
  );
}

function ProductImageFrame({ image, alt, primary = false }: { image: ProductImage; alt: string; primary?: boolean }) {
  return (
    <figure className="relative mx-auto aspect-[4/5] w-full max-w-[34rem] overflow-hidden border-y border-line/70 bg-surface-subtle sm:rounded-2xl sm:border">
      <Image
        src={image.imageUrl}
        alt={alt}
        fill
        preload={primary}
        className="object-cover"
        sizes={primary
          ? "(min-width: 1280px) 34rem, (min-width: 1024px) 50vw, 100vw"
          : "(min-width: 1024px) 17rem, 100vw"}
      />
    </figure>
  );
}
