import Link from "next/link";
import { hasProductFilters, productStatusOptions } from "@/modules/products/query";
import { ProductThumbnail } from "@/modules/products/components/product-thumbnail";
import { ProductRowActions } from "@/modules/products/components/product-row-actions";
import type { PagedResult, Product, ProductListQuery } from "@/modules/products/types";

const statusClasses: Record<number, string> = {
  0: "border-blue-200 bg-blue-50 text-blue-800",
  1: "border-emerald-200 bg-emerald-50 text-emerald-800",
  2: "border-amber-200 bg-amber-50 text-amber-800",
  3: "border-slate-300 bg-slate-100 text-slate-700",
};

const currencyFormatter = new Intl.NumberFormat("tr-TR", {
  style: "currency",
  currency: "TRY",
  maximumFractionDigits: 2,
});

// Burada ürün satırını görsel, durum, envanter ve organizasyon hiyerarşisi güçlü bir yönetim tablosunda gösteriyorum.
export function ProductTable({
  page,
  query,
}: {
  page: PagedResult<Product>;
  query: ProductListQuery;
}) {
  if (page.items.length === 0) {
    return (
      <div className="px-5 py-14 text-center">
        <h2 className="text-base font-semibold text-foreground">
          {hasProductFilters(query) ? "Filtrelere uyan ürün bulunamadı" : "Henüz ürün bulunmuyor"}
        </h2>
        <p className="mx-auto mt-2 max-w-lg text-sm leading-6 text-muted">
          {hasProductFilters(query)
            ? "Arama veya filtreleri değiştirerek tekrar deneyin."
            : "İlk ürünü oluşturmak için sayfanın üstündeki Ürün ekle aksiyonunu kullanın."}
        </p>
      </div>
    );
  }

  return (
    <div className="overflow-x-auto bg-surface-strong">
      <table className="w-full min-w-[940px] table-fixed border-collapse text-left text-sm">
        <colgroup>
          <col className="w-[36%]" />
          <col className="w-[12%]" />
          <col className="w-[17%]" />
          <col className="w-[14%]" />
          <col className="w-[16%]" />
          <col className="w-[5%]" />
        </colgroup>
        <thead className="border-b border-border bg-surface-subtle/80 text-[11px] font-bold uppercase tracking-[0.08em] text-muted">
          <tr>
            <th scope="col" className="px-4 py-2.5">Ürün</th>
            <th scope="col" className="px-3 py-2.5">Durum</th>
            <th scope="col" className="px-3 py-2.5 text-right">Fiyat</th>
            <th scope="col" className="px-3 py-2.5">Toplam stok</th>
            <th scope="col" className="px-3 py-2.5">Organizasyon</th>
            <th scope="col" aria-label="Ürün işlemleri" className="px-1.5 py-2.5" />
          </tr>
        </thead>
        <tbody className="divide-y divide-border/80">
          {page.items.map((product) => {
            const status = productStatusOptions.find((option) => option.value === product.status);
            const totalStock = product.variants.reduce((sum, variant) => sum + variant.stock, 0);
            const productHref = `/products/${encodeURIComponent(product.id)}`;
            // Burada liste DTO'sunun tek sorguda döndürdüğü ana görseli kullanarak ürün başına ek API isteğini önlüyorum.
            const mainImage = product.mainImage;

            return (
              <tr key={product.id} className="group bg-surface-strong align-middle transition-colors hover:bg-primary-soft/30">
                <td className="px-4 py-2.5">
                  <Link href={productHref} className="flex min-w-0 items-center gap-2.5 rounded-lg outline-none focus-visible:ring-2 focus-visible:ring-focus focus-visible:ring-offset-2">
                    <ProductThumbnail src={mainImage?.imageUrl} alt={mainImage?.altText || product.title} />
                    <span className="min-w-0">
                      <span className="block truncate text-sm font-bold leading-5 text-foreground transition-colors group-hover:text-primary">
                        {product.title}
                      </span>
                      <span className="mt-1 flex min-w-0 flex-wrap items-center gap-x-2 gap-y-1 text-xs text-muted">
                        <span className="font-mono font-medium text-foreground/75">{product.mainSku}</span>
                        <span aria-hidden="true" className="text-border-strong">•</span>
                        <span>{product.id}</span>
                        {product.isFeatured ? (
                          <span className="rounded-md bg-primary-soft px-1.5 py-0.5 font-semibold text-primary">Öne çıkan</span>
                        ) : null}
                      </span>
                    </span>
                  </Link>
                </td>
                <td className="px-3 py-2.5">
                  <span className={`inline-flex rounded-md border px-2 py-1 text-xs font-bold ${statusClasses[product.status] || statusClasses[3]}`}>
                    {status?.label || "Bilinmiyor"}
                  </span>
                </td>
                {/* Burada belgeli varyant satış fiyatlarından yalnız gösterim amaçlı fiyat aralığını sunuyorum. */}
                <td className="px-3 py-2.5 text-right">
                  <p className="whitespace-nowrap font-bold tabular-nums text-foreground">{formatPriceRange(product)}</p>
                  <CompareAtPrice product={product} />
                </td>
                <td className="px-3 py-2.5">
                  <p className={`font-bold tabular-nums ${stockTextClass(totalStock)}`}>{totalStock} adet</p>
                  <p className="mt-1 text-xs text-muted">
                    {product.variants.length > 0
                      ? `${product.variants.length} ${product.hasVariants ? "varyant" : "satış kaydı"}`
                      : "Varyant yok"}
                  </p>
                </td>
                <td className="px-3 py-2.5">
                  <p className="font-semibold text-foreground">{product.typeName || "Tür atanmamış"}</p>
                  <p className="mt-1 text-xs text-muted">{product.brandName || "Marka atanmamış"}</p>
                </td>
                <td className="px-1.5 py-2.5 text-right">
                  <ProductRowActions id={product.id} title={product.title} status={product.status} />
                </td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}

// Burada toplam stok değerini tükenmiş, düşük ve yeterli durumlarına göre okunabilir renkle vurguluyorum.
function stockTextClass(totalStock: number): string {
  if (totalStock === 0) return "text-danger";
  if (totalStock < 10) return "text-warning";
  return "text-success";
}

// Burada varyant fiyatlarının eksik olduğu üründe yanıltıcı fiyat yerine açık bir durum metni gösteriyorum.
function formatPriceRange(product: Product): string {
  return formatCurrencyRange(product.variants.map((variant) => variant.price)) ?? "Fiyat yok";
}

// Burada karşılaştırma fiyatını yalnız geçerli satış fiyatından yüksek olan varyantlar için gösteriyorum.
function CompareAtPrice({ product }: { product: Product }) {
  const compareAtPrices = product.variants
    .filter((variant) => variant.compareAtPrice != null && variant.compareAtPrice > variant.price)
    .map((variant) => variant.compareAtPrice as number);
  const formattedRange = formatCurrencyRange(compareAtPrices);

  if (!formattedRange) return null;

  return <p className="mt-1 whitespace-nowrap text-xs text-muted"><span>Eski: </span><s className="tabular-nums">{formattedRange}</s></p>;
}

// Burada tek ve çok varyantlı ürünlerde para aralığını aynı biçimde gösteriyorum.
function formatCurrencyRange(prices: number[]): string | null {
  if (prices.length === 0) return null;

  const minimum = Math.min(...prices);
  const maximum = Math.max(...prices);
  return minimum === maximum ? currencyFormatter.format(minimum) : `${currencyFormatter.format(minimum)} – ${currencyFormatter.format(maximum)}`;
}
