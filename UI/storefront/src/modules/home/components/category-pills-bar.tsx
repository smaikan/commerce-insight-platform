"use client";

import { useRef } from "react";
import Image from "next/image";
import Link from "next/link";
import type { CategoryShowcaseItem } from "@/modules/catalog/categories";

// Burada ana sayfada kategorilere tek bakışta ulaşımı sağlayan dairesel hızlı keşif barını sunuyorum.
export function CategoryPillsBar({ categories }: { categories: CategoryShowcaseItem[] }) {
  const scrollContainerRef = useRef<HTMLDivElement>(null);

  const scrollLeft = () => {
    if (scrollContainerRef.current) {
      scrollContainerRef.current.scrollBy({ left: -240, behavior: "smooth" });
    }
  };

  const scrollRight = () => {
    if (scrollContainerRef.current) {
      scrollContainerRef.current.scrollBy({ left: 240, behavior: "smooth" });
    }
  };

  if (!categories || categories.length === 0) return null;

  return (
    <section aria-label="Popüler kategoriler" className="home-shell pt-8 pb-4 sm:pt-10 sm:pb-6">
      <div className="flex items-center justify-between border-b border-line/60 pb-3">
        <div className="flex items-center gap-2">
          <span className="size-1.5 rounded-full bg-brand-700" />
          <h2 className="text-xs font-bold uppercase tracking-[0.2em] text-brand-700">
            Kategorilere Göre Keşfet
          </h2>
        </div>
        <Link
          href="/categories"
          prefetch={false}
          className="focus-ring cursor-pointer text-xs font-semibold text-brand-700 hover:text-brand-950 transition-colors"
        >
          Tüm Kategoriler <span aria-hidden="true">&rarr;</span>
        </Link>
      </div>

      <div className="relative group mt-5">
        <div
          ref={scrollContainerRef}
          className="-mx-4 px-4 sm:-mx-0 sm:px-0 flex items-start gap-4 sm:gap-6 overflow-x-auto pb-3 scroll-smooth scrollbar-none"
        >
          {categories.map((category) => (
            <Link
              key={category.id}
              href={category.href}
              prefetch={false}
              className="focus-ring group/pill flex flex-col items-center gap-2.5 shrink-0 text-center w-18 sm:w-22 cursor-pointer"
            >
              <div className="relative size-16 sm:size-20 overflow-hidden rounded-full border-2 border-line/80 bg-surface-subtle shadow-xs ring-2 ring-transparent transition-all duration-300 group-hover/pill:scale-105 group-hover/pill:border-brand-700 group-hover/pill:ring-brand-700/20 group-hover/pill:shadow-md">
                {category.imageUrl ? (
                  <Image
                    src={category.imageUrl}
                    alt={category.imageAlt || category.name}
                    fill
                    sizes="(min-width: 640px) 80px, 64px"
                    className="object-cover transition-transform duration-700 ease-out group-hover/pill:scale-110"
                  />
                ) : (
                  <div className="flex size-full items-center justify-center bg-linear-to-br from-surface to-surface-subtle text-brand-700">
                    <CategoryFallbackIcon name={category.name} />
                  </div>
                )}
              </div>
              <span className="w-full whitespace-normal text-xs font-semibold leading-4 text-ink transition-colors group-hover/pill:text-brand-700">
                {category.name}
              </span>
            </Link>
          ))}
        </div>

        {categories.length > 4 && (
          <>
            <button
              type="button"
              onClick={scrollLeft}
              className="absolute -left-2 sm:-left-3 top-1/2 -translate-y-1/2 flex size-8 sm:size-9 cursor-pointer items-center justify-center rounded-full bg-surface shadow-md text-ink hover:bg-surface-subtle focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-700 opacity-0 group-hover:opacity-100 transition-all duration-200 border border-line z-10 hover:scale-105 active:scale-95"
              aria-label="Önceki kategoriler"
            >
              <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.2" strokeLinecap="round" strokeLinejoin="round"><path d="m15 18-6-6 6-6"/></svg>
            </button>
            <button
              type="button"
              onClick={scrollRight}
              className="absolute -right-2 sm:-right-3 top-1/2 -translate-y-1/2 flex size-8 sm:size-9 cursor-pointer items-center justify-center rounded-full bg-surface shadow-md text-ink hover:bg-surface-subtle focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-700 opacity-0 group-hover:opacity-100 transition-all duration-200 border border-line z-10 hover:scale-105 active:scale-95"
              aria-label="Sonraki kategoriler"
            >
              <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.2" strokeLinecap="round" strokeLinejoin="round"><path d="m9 18 6-6-6-6"/></svg>
            </button>
          </>
        )}
      </div>
    </section>
  );
}

// Burada görseli olmayan kategoriler için türe uygun zarif takı/aksesuar vektör simgesi döndürüyorum.
function CategoryFallbackIcon({ name }: { name: string }) {
  const lower = name.toLowerCase();

  if (lower.includes("kolye") || lower.includes("choker")) {
    return (
      <svg aria-hidden="true" viewBox="0 0 24 24" className="size-7" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
        <path d="M6 3c0 7 3 13 6 13s6-6 6-13" />
        <circle cx="12" cy="18" r="2" />
      </svg>
    );
  }

  if (lower.includes("çanta")) {
    return (
      <svg aria-hidden="true" viewBox="0 0 24 24" className="size-7" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
        <path d="M6 9h12v11a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2V9Z" />
        <path d="M9 9V6a3 3 0 0 1 6 0v3" />
      </svg>
    );
  }

  if (lower.includes("fular") || lower.includes("şapka") || lower.includes("broş")) {
    return (
      <svg aria-hidden="true" viewBox="0 0 24 24" className="size-7" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
        <path d="m12 3-8 9h16l-8-9Z" />
        <path d="M4 12v6a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2v-6" />
      </svg>
    );
  }

  return (
    <svg aria-hidden="true" viewBox="0 0 24 24" className="size-7" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
      <path d="m12 3 8 6-8 12-8-12 8-6Z" />
    </svg>
  );
}
