"use client";

import { useRef } from "react";
import Link from "next/link";
import Image from "next/image";
import type { CollectionShowcaseItem } from "@/modules/catalog/collections";

// Burada koleksiyon vitrinini yatay kaydırılabilir, lüks ve erişilebilir bir kart şeridi olarak sunuyorum.
export function CollectionCarousel({ collections }: { collections: CollectionShowcaseItem[] }) {
  const scrollContainerRef = useRef<HTMLDivElement>(null);

  const scrollLeft = () => {
    if (scrollContainerRef.current) {
      scrollContainerRef.current.scrollBy({ left: -320, behavior: "smooth" });
    }
  };

  const scrollRight = () => {
    if (scrollContainerRef.current) {
      scrollContainerRef.current.scrollBy({ left: 320, behavior: "smooth" });
    }
  };

  if (!collections || collections.length === 0) return null;

  return (
    <div className="relative group mx-auto w-full">
      <div 
        ref={scrollContainerRef}
        className="mt-6 flex snap-x snap-mandatory gap-5 overflow-x-auto pb-4 scroll-smooth w-full scrollbar-none"
      >
        {collections.map((collection) => (
          <Link
            key={collection.id}
            href={collection.href}
            prefetch={false}
            className="group/item relative w-60 sm:w-72 shrink-0 snap-start overflow-hidden rounded-2xl bg-surface-subtle border border-line/60 shadow-xs outline-none focus-visible:ring-2 focus-visible:ring-brand-700 focus-visible:ring-offset-2 transition-transform duration-300 hover:-translate-y-1 hover:shadow-md"
          >
            <div className="aspect-[4/5] w-full overflow-hidden relative">
              {collection.imageUrl ? (
                <Image
                  src={collection.imageUrl}
                  alt={collection.imageAlt}
                  fill
                  className="object-cover transition-transform duration-700 group-hover/item:scale-105"
                  sizes="(min-width: 1024px) 288px, 240px"
                />
              ) : (
                <div className="flex size-full flex-col items-center justify-center gap-3 bg-gradient-to-br from-brand-950 via-brand-700 to-brand-600 p-6 text-center text-white">
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
                    ELEVEN EDİT
                  </span>
                </div>
              )}
            </div>
            {/* Gradient Overlay & Text */}
            <div className="absolute inset-0 bg-gradient-to-t from-black/75 via-black/20 to-transparent pointer-events-none" />
            <div className="absolute bottom-0 left-0 p-5 w-full">
              <span className="text-[0.6875rem] font-bold uppercase tracking-[0.15em] text-footer-icon block mb-1">
                KOLEKSİYON
              </span>
              <h3 className="text-base sm:text-lg font-bold text-white drop-shadow-md">
                {collection.name}
              </h3>
              <p className="text-xs text-white/80 mt-0.5">
                {collection.productCount > 0 ? `${collection.productCount} Parça` : "Koleksiyonu İncele"} &rarr;
              </p>
            </div>
          </Link>
        ))}
      </div>

      {collections.length > 3 && (
        <>
          <button
            type="button"
            onClick={scrollLeft}
            className="absolute -left-3 top-1/2 -translate-y-1/2 flex size-10 cursor-pointer items-center justify-center rounded-full bg-surface shadow-md text-ink hover:bg-surface-subtle focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-700 opacity-0 group-hover:opacity-100 transition-all duration-200 border border-line z-10 hover:scale-105 active:scale-95"
            aria-label="Önceki koleksiyonlar"
          >
            <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="m15 18-6-6 6-6"/></svg>
          </button>
          <button
            type="button"
            onClick={scrollRight}
            className="absolute -right-3 top-1/2 -translate-y-1/2 flex size-10 cursor-pointer items-center justify-center rounded-full bg-surface shadow-md text-ink hover:bg-surface-subtle focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-700 opacity-0 group-hover:opacity-100 transition-all duration-200 border border-line z-10 hover:scale-105 active:scale-95"
            aria-label="Sonraki koleksiyonlar"
          >
            <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="m9 18 6-6-6-6"/></svg>
          </button>
        </>
      )}
    </div>
  );
}
