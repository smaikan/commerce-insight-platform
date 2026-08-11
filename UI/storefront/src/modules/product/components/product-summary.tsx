import { formatCurrency } from "@/lib/formatting/currency";
import { siteConfig } from "@/lib/site-config";
import { ProductPurchasePanel } from "@/modules/product/components/product-purchase-panel";
import type { Product } from "@/modules/product/types";

// Burada ürünün gerçek fiyat, varyant ve stok bilgisini referans tasarımdan bağımsız sade bir satın alma hiyerarşisinde sunuyorum.
export function ProductSummary({ product }: { product: Product }) {
  const activeVariants = product.variants.filter((variant) => variant.isActive);
  const purchaseVariants = activeVariants.map(({ id, name, value, price, stock }) => ({ id, name, value, price, stock }));
  const isOutOfStock = !activeVariants.some((variant) => variant.stock > 0);
  const prices = activeVariants.map((variant) => variant.price);
  const minimumPrice = prices.length > 0 ? Math.min(...prices) : null;
  const maximumPrice = prices.length > 0 ? Math.max(...prices) : null;
  const materials = Array.from(
    new Set(activeVariants.map((variant) => variant.material?.trim()).filter((value): value is string => Boolean(value))),
  );

  return (
    <aside className="lg:col-start-2 lg:row-start-1 lg:self-start">
      {product.brandName ? <p className="text-xs font-bold tracking-[0.12em] text-brand-700 uppercase">{product.brandName}</p> : null}
      <h1 className="mt-3 text-3xl font-semibold leading-[1.08] tracking-[-0.04em] text-ink sm:text-4xl">{product.title}</h1>

      {product.ratingCount > 0 ? (
        <p className="mt-4 text-sm text-ink-muted" aria-label={`${product.averageRating} puan, ${product.ratingCount} değerlendirme`}>
          <span className="text-brand-700" aria-hidden="true">★</span> {product.averageRating.toFixed(1)} · {product.ratingCount} değerlendirme
        </p>
      ) : null}

      <div className="mt-7 border-y border-line py-5">
        {minimumPrice !== null ? (
          <p className="text-2xl font-bold text-ink">
            {formatCurrency(minimumPrice)}
            {maximumPrice !== null && maximumPrice !== minimumPrice ? <span className="ml-2 text-sm font-medium text-ink-muted">başlayan fiyatla</span> : null}
          </p>
        ) : <p className="text-sm text-ink-muted">Fiyat bilgisi bulunmuyor</p>}
        {isOutOfStock ? <p className="mt-2 text-sm font-semibold text-danger">Şu anda stokta yok</p> : null}
      </div>

      <ProductPurchasePanel variants={purchaseVariants} currency={siteConfig.currency} showVariantSelection={product.hasVariants} />

      {product.description ? (
        <details className="mt-7 border-t border-line py-5" open>
          <summary className="focus-ring cursor-pointer text-sm font-bold text-ink">Ürün açıklaması</summary>
          <p className="mt-4 whitespace-pre-line text-sm leading-7 text-ink-muted">{product.description}</p>
        </details>
      ) : null}

      {materials.length > 0 ? (
        <details className="border-t border-line py-5">
          <summary className="focus-ring cursor-pointer text-sm font-bold text-ink">Materyal</summary>
          <p className="mt-4 text-sm leading-6 text-ink-muted">{materials.join(", ")}</p>
        </details>
      ) : null}
    </aside>
  );
}
