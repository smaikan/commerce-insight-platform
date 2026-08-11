"use client";

import { useEffect, useState } from "react";

// Burada yalnız mobil carousel göstergesini yöneten küçük etkileşim sınırını tutuyorum; görseller Server Component olarak kalıyor.
export function ProductCarouselControls({ carouselId, count }: { carouselId: string; count: number }) {
  const [activeIndex, setActiveIndex] = useState(0);

  useEffect(() => {
    if (count <= 1) return;

    const carousel = document.getElementById(carouselId);
    if (!carousel) return;

    let animationFrame = 0;
    const updateActiveIndex = () => {
      cancelAnimationFrame(animationFrame);
      animationFrame = requestAnimationFrame(() => {
        const slides = Array.from(carousel.querySelectorAll<HTMLElement>("[data-carousel-slide]"));
        const nearestIndex = slides.reduce((bestIndex, slide, index) => (
          Math.abs(slide.offsetLeft - carousel.scrollLeft) < Math.abs(slides[bestIndex].offsetLeft - carousel.scrollLeft)
            ? index
            : bestIndex
        ), 0);
        setActiveIndex(nearestIndex);
      });
    };

    carousel.addEventListener("scroll", updateActiveIndex, { passive: true });
    return () => {
      carousel.removeEventListener("scroll", updateActiveIndex);
      cancelAnimationFrame(animationFrame);
    };
  }, [carouselId, count]);

  if (count <= 1) return null;

  function goToSlide(index: number) {
    const carousel = document.getElementById(carouselId);
    const slide = carousel?.querySelectorAll<HTMLElement>("[data-carousel-slide]")[index];
    if (!carousel || !slide) return;

    carousel.scrollTo({
      left: slide.offsetLeft,
      behavior: window.matchMedia("(prefers-reduced-motion: reduce)").matches ? "auto" : "smooth",
    });
  }

  return (
    <nav
      aria-label="Ürün görseli seçimi"
      className="-mx-4 flex min-h-14 items-center justify-center gap-1 overflow-x-auto border-b border-line bg-surface px-4 sm:mx-0 sm:rounded-b-xl lg:hidden"
    >
      {Array.from({ length: count }, (_, index) => (
        <button
          key={index}
          type="button"
          aria-label={`${index + 1}. görseli göster`}
          aria-current={activeIndex === index ? "true" : undefined}
          onClick={() => goToSlide(index)}
          className="focus-ring flex size-8 shrink-0 items-center justify-center"
        >
          <span
            aria-hidden="true"
            className={`block rounded-full transition-[width,height,background-color] ${
              activeIndex === index ? "size-2.5 bg-brand-950" : "size-2 bg-ink-muted/45"
            }`}
          />
        </button>
      ))}
    </nav>
  );
}
