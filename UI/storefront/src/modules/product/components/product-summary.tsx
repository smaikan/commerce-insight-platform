"use client";

import { useState } from "react";

import { formatCurrency } from "@/lib/formatting/currency";
import { siteConfig } from "@/lib/site-config";
import { FavoriteButton } from "@/modules/favorites/components/favorite-button";
import { ProductInstallmentTable } from "@/modules/product/components/product-installment-table";
import { ProductPurchasePanel } from "@/modules/product/components/product-purchase-panel";
import type { Product } from "@/modules/product/types";

// Burada ürünün gerçek fiyat, varyant ve stok bilgisini referans tasarımdan bağımsız sade bir satın alma hiyerarşisinde sunuyorum.
export function ProductSummary({ product }: { product: Product }) {
  const activeVariants = product.variants.filter((variant) => variant.isActive);
  const purchaseVariants = activeVariants.map(({ id, name, value, price, stock }) => ({ id, name, value, price, stock }));
  const firstAvailable = purchaseVariants.find((variant) => variant.stock > 0);
  const [selectedVariantId, setSelectedVariantId] = useState(firstAvailable?.id || purchaseVariants[0]?.id || "");

  const selectedVariant = purchaseVariants.find((variant) => variant.id === selectedVariantId);
  const isOutOfStock = !activeVariants.some((variant) => variant.stock > 0);
  const prices = activeVariants.map((variant) => variant.price);
  const minimumPrice = prices.length > 0 ? Math.min(...prices) : null;
  const currentPrice = selectedVariant?.price ?? minimumPrice;

  const materials = Array.from(
    new Set(activeVariants.map((variant) => variant.material?.trim()).filter((value): value is string => Boolean(value))),
  );

  return (
    <aside className="w-full min-w-0">
      {product.brandName ? <p className="text-xs font-bold tracking-[0.12em] text-brand-700 uppercase">{product.brandName}</p> : null}
      <div className="mt-2.5 flex items-start gap-2 sm:gap-3">
        <h1 className="min-w-0 pt-0.5 text-3xl font-semibold leading-[1.08] tracking-[-0.04em] text-ink sm:text-4xl">{product.title}</h1>
        <FavoriteButton productId={product.id} productTitle={product.title} variant="detail" />
      </div>

      <div className="mt-7 border-y border-line py-5">
        {currentPrice !== null ? (
          <p className="text-2xl font-bold text-ink">
            {formatCurrency(currentPrice)}
          </p>
        ) : <p className="text-sm text-ink-muted">Fiyat bilgisi bulunmuyor</p>}
        {isOutOfStock ? <p className="mt-2 text-sm font-semibold text-danger">Şu anda stokta yok</p> : null}
      </div>

      {/* 1. Satın Alma / Varyant Seçim Paneli */}
      <ProductPurchasePanel
        variants={purchaseVariants}
        currency={siteConfig.currency}
        showVariantSelection={product.hasVariants}
        selectedId={selectedVariantId}
        onSelectVariant={setSelectedVariantId}
      />

      {/* 2. Ürün Açıklaması */}
      {product.description ? (
        <details className="mt-7 border-t border-line py-5" open>
          <summary className="focus-ring cursor-pointer text-sm font-bold text-ink">Ürün açıklaması</summary>
          <p className="mt-4 whitespace-pre-line text-sm leading-7 text-ink-muted">{product.description}</p>
        </details>
      ) : null}

      {/* 3. Materyal */}
      {materials.length > 0 ? (
        <details className="border-t border-line py-5">
          <summary className="focus-ring cursor-pointer text-sm font-bold text-ink">Materyal</summary>
          <p className="mt-4 text-sm leading-6 text-ink-muted">{materials.join(", ")}</p>
        </details>
      ) : null}

      {/* 4. İyzico Banka Bazlı Taksit Tablosu (Ürün açıklaması ve materyalin altında) */}
      {currentPrice !== null ? (
        <ProductInstallmentTable price={currentPrice} currency={siteConfig.currency} />
      ) : null}
    </aside>
  );
}
