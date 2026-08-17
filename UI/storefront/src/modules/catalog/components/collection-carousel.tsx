"use client";

import { useRef } from "react";
import Link from "next/link";
import Image from "next/image";
import type { CollectionShowcaseItem } from "@/modules/catalog/collections";

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

  return (
    <div className="relative group mx-auto w-max max-w-full">
      <div 
        ref={scrollContainerRef}
        className="mt-8 flex snap-x snap-mandatory gap-6 overflow-x-auto pb-4 scroll-smooth w-full"
      >
        {collections.map((collection) => (
          <Link
            key={collection.id}
            href={collection.href}
            className="group/item relative w-64 shrink-0 snap-start overflow-hidden rounded-2xl bg-surface-subtle outline-none focus-visible:ring-2 focus-visible:ring-brand-700 focus-visible:ring-offset-2"
          >
            <div className="aspect-[4/5] w-full overflow-hidden">
              {collection.imageUrl ? (
                <Image
                  src={collection.imageUrl}
                  alt={collection.imageAlt}
                  fill
                  className="object-cover transition-transform duration-500 group-hover/item:scale-105"
                  sizes="(min-width: 1024px) 256px, 256px"
                />
              ) : (
                <div className="flex size-full items-center justify-center bg-line/20 text-ink-muted">Görsel Yok</div>
              )}
            </div>
            <div className="absolute inset-0 bg-gradient-to-t from-black/60 via-black/10 to-transparent" />
            <div className="absolute bottom-0 left-0 p-5 w-full">
              <h3 className="text-lg font-semibold text-white drop-shadow-md">{collection.name}</h3>
            </div>
          </Link>
        ))}
      </div>

      {collections.length > 4 && (
        <>
          <button
            onClick={scrollLeft}
            className="absolute -left-4 top-1/2 -translate-y-1/2 flex size-10 items-center justify-center rounded-full bg-white shadow-md text-ink hover:bg-surface-subtle focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-700 opacity-0 group-hover:opacity-100 transition-opacity disabled:opacity-0"
            aria-label="Önceki"
          >
            <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="m15 18-6-6 6-6"/></svg>
          </button>
          <button
            onClick={scrollRight}
            className="absolute -right-4 top-1/2 -translate-y-1/2 flex size-10 items-center justify-center rounded-full bg-white shadow-md text-ink hover:bg-surface-subtle focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-700 opacity-0 group-hover:opacity-100 transition-opacity disabled:opacity-0"
            aria-label="Sonraki"
          >
            <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="m9 18 6-6-6-6"/></svg>
          </button>
        </>
      )}
    </div>
  );
}
