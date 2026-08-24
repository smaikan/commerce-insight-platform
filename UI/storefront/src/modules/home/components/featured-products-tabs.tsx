"use client";

import { useState } from "react";
import Link from "next/link";
import { ProductCard } from "@/modules/catalog/components/product-card";
import type { PublishedProduct } from "@/modules/catalog/types";

type TabKey = "best-sellers" | "new-arrivals" | "special-offers";

interface FeaturedProductsTabsProps {
  bestSellers: PublishedProduct[];
  newArrivals: PublishedProduct[];
  discountedProducts?: PublishedProduct[];
}

// Burada popüler, yeni gelen ve fırsat ürünlerini sekmeler arasında anında geçişle sunan ürün vitrinini yönetiyorum.
export function FeaturedProductsTabs({
  bestSellers,
  newArrivals,
  discountedProducts = [],
}: FeaturedProductsTabsProps) {
  const [activeTab, setActiveTab] = useState<TabKey>("best-sellers");

  const tabs: Array<{ key: TabKey; label: string; href: string }> = [
    { key: "best-sellers", label: "En Çok Satanlar", href: "/products?sort=popular" },
    { key: "new-arrivals", label: "Yeni Gelenler", href: "/products?sort=newest" },
    ...(discountedProducts.length > 0
      ? [{ key: "special-offers" as TabKey, label: "Fırsat Ürünleri", href: "/products" }]
      : []),
  ];

  const currentProducts =
    activeTab === "best-sellers"
      ? bestSellers
      : activeTab === "new-arrivals"
      ? newArrivals
      : discountedProducts;

  const currentHref = tabs.find((t) => t.key === activeTab)?.href || "/products";

  if (!currentProducts || currentProducts.length === 0) return null;

  return (
    <section aria-labelledby="featured-products-heading" className="home-shell py-10 sm:py-14">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between border-b border-line pb-4">
        <div>
          <p className="text-xs font-bold uppercase tracking-[0.18em] text-brand-700">
            ÖNE ÇIKAN MODELLER
          </p>
          <h2 id="featured-products-heading" className="mt-1 text-2xl font-bold tracking-tight text-ink sm:text-3xl">
            Stilinizi Tamamlayacak Parçalar
          </h2>
        </div>

        {/* Sekme Butonları */}
        <div className="flex items-center gap-1 sm:gap-2 bg-surface-subtle p-1 rounded-xl border border-line/60 self-start sm:self-auto">
          {tabs.map((tab) => {
            const isActive = activeTab === tab.key;
            return (
              <button
                key={tab.key}
                type="button"
                onClick={() => setActiveTab(tab.key)}
                className={`focus-ring px-3.5 py-1.5 text-xs sm:text-sm font-semibold rounded-lg transition-all duration-200 ${
                  isActive
                    ? "bg-brand-950 text-white shadow-xs"
                    : "text-ink-muted hover:text-ink hover:bg-surface/60"
                }`}
                aria-pressed={isActive}
              >
                {tab.label}
              </button>
            );
          })}
        </div>
      </div>

      {/* Ürün Izgarası: Ana sayfada hero banner LCP önceliğini korumak için ürün kartları lazy olarak yüklenir */}
      <div className="mt-8 grid grid-cols-2 gap-x-4 gap-y-8 sm:gap-x-6 sm:gap-y-10 md:grid-cols-4 lg:gap-x-8">
        {currentProducts.slice(0, 8).map((product) => (
          <ProductCard key={product.id} product={product} />
        ))}
      </div>

      {/* Tümünü Gör Butonu */}
      <div className="mt-10 flex justify-center">
        <Link
          href={currentHref}
          prefetch={false}
          className="focus-ring inline-flex items-center gap-2 rounded-xl border-2 border-brand-950 px-7 py-3 text-sm font-bold text-brand-950 transition-all hover:bg-brand-950 hover:text-white"
        >
          <span>Tüm {tabs.find((t) => t.key === activeTab)?.label} Gör</span>
          <span aria-hidden="true">&rarr;</span>
        </Link>
      </div>
    </section>
  );
}
