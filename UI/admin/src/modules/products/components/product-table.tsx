import Link from "next/link";
import { hasProductFilters, productStatusOptions } from "@/modules/products/query";
import { ProductThumbnail } from "@/modules/products/components/product-thumbnail";
import type { PagedResult, Product, ProductImage, ProductListQuery } from "@/modules/products/types";

const statusClasses: Record<number, string> = {
  0: "border-blue-200 bg-blue-50 text-blue-800",
  1: "border-emerald-200 bg-emerald-50 text-emerald-800",
  2: "border-amber-200 bg-amber-50 text-amber-800",
  3: "border-slate-300 bg-slate-100 text-slate-700",
};

// Burada ürün satırını görsel, durum, envanter ve organizasyon hiyerarşisi güçlü bir yönetim tablosunda gösteriyorum.
export function ProductTable({
  page,
  query,
  mainImages,
}: {
  page: PagedResult<Product>;
  query: ProductListQuery;
  mainImages: Record<string, ProductImage | null>;
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
      <table className="w-full min-w-[1040px] border-collapse text-left text-sm">
        <thead className="border-b border-border bg-surface-subtle/80 text-[11px] font-bold uppercase tracking-[0.08em] text-muted">
          <tr>
            <th scope="col" className="w-[34%] px-5 py-3.5">Ürün</th>
            <th scope="col" className="px-4 py-3.5">Durum</th>
            <th scope="col" className="px-4 py-3.5">Envanter</th>
            <th scope="col" className="px-4 py-3.5">Organizasyon</th>
            <th scope="col" className="px-4 py-3.5">Etiketler</th>
            <th scope="col" className="w-12 px-4 py-3.5"><span className="sr-only">İşlem</span></th>
          </tr>
        </thead>
        <tbody className="divide-y divide-border/80">
          {page.items.map((product) => {
            const status = productStatusOptions.find((option) => option.value === product.status);
            const totalStock = product.variants.reduce((sum, variant) => sum + variant.stock, 0);
            const productHref = `/products/${encodeURIComponent(product.id)}`;
            const mainImage = mainImages[product.id];

            return (
              <tr key={product.id} className="group bg-surface-strong align-middle transition-colors hover:bg-primary-soft/30">
                <td className="px-5 py-3">
                  <Link href={productHref} className="flex min-w-0 items-center gap-3 rounded-lg outline-none focus-visible:ring-2 focus-visible:ring-focus focus-visible:ring-offset-2">
                    <ProductThumbnail src={mainImage?.imageUrl} alt={mainImage?.altText || product.title} />
                    <span className="min-w-0">
                      <span className="block truncate text-[15px] font-bold leading-5 text-foreground transition-colors group-hover:text-primary">
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
                <td className="px-4 py-3">
                  <span className={`inline-flex rounded-md border px-2 py-1 text-xs font-bold ${statusClasses[product.status] || statusClasses[3]}`}>
                    {status?.label || "Bilinmiyor"}
                  </span>
                </td>
                <td className="px-4 py-3">
                  <p className={`font-bold tabular-nums ${stockTextClass(totalStock)}`}>{totalStock} adet</p>
                  <p className="mt-1 text-xs text-muted">
                    {product.variants.length > 0
                      ? `${product.variants.length} ${product.hasVariants ? "varyant" : "satış kaydı"}`
                      : "Varyant yok"}
                  </p>
                </td>
                <td className="px-4 py-3">
                  <p className="font-semibold text-foreground">{product.typeName || "Tür atanmamış"}</p>
                  <p className="mt-1 text-xs text-muted">{product.brandName || "Marka atanmamış"}</p>
                </td>
                <td className="max-w-56 px-4 py-3">
                  {product.tags.length > 0 ? (
                    <div className="flex flex-wrap gap-1.5">
                      {product.tags.slice(0, 2).map((tag) => (
                        <span key={tag.id} className="max-w-28 truncate rounded-md border border-border bg-surface-subtle px-1.5 py-0.5 text-xs font-medium text-muted">
                          {tag.name}
                        </span>
                      ))}
                      {product.tags.length > 2 ? <span className="self-center text-xs font-semibold text-muted">+{product.tags.length - 2}</span> : null}
                    </div>
                  ) : <span className="text-xs text-muted">Etiket yok</span>}
                </td>
                <td className="px-4 py-3 text-right">
                  <Link
                    href={productHref}
                    aria-label={`${product.title} ürününü düzenle`}
                    className="inline-flex size-9 items-center justify-center rounded-lg border border-transparent text-muted transition-colors hover:border-border hover:bg-surface-strong hover:text-primary focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-focus"
                  >
                    <svg aria-hidden="true" viewBox="0 0 20 20" className="size-4 fill-none stroke-current stroke-2">
                      <path d="m7 4 6 6-6 6" strokeLinecap="round" strokeLinejoin="round" />
                    </svg>
                  </Link>
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
