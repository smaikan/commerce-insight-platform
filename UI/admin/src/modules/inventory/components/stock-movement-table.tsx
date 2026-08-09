import Link from "next/link";
import { buildStockMovementListHref, hasStockMovementFilters } from "@/modules/inventory/query";
import { stockMovementDirectionLabel, stockMovementTypeLabel } from "@/modules/inventory/stock-movement-rules";
import type { StockMovementListQuery, StockMovementPage } from "@/modules/inventory/types";

// Burada stok hareketlerini ürün bağlamı, imzalı miktar ve denetlenebilir bakiye değişimiyle gösteriyorum.
export function StockMovementTable({ page, query }: { page: StockMovementPage; query: StockMovementListQuery }) {
  if (page.items.length === 0) {
    return (
      <div className="px-5 py-14 text-center">
        <h2 className="text-base font-semibold text-foreground">{hasStockMovementFilters(query) ? "Filtrelere uyan stok hareketi bulunamadı" : "Henüz stok hareketi bulunmuyor"}</h2>
        <p className="mx-auto mt-2 max-w-xl text-sm leading-6 text-muted">{hasStockMovementFilters(query) ? "Arama veya filtreleri değiştirerek yeniden deneyin." : "İlk manuel hareketi kaydetmek için sağ üstteki Hareket oluştur aksiyonunu kullanın."}</p>
      </div>
    );
  }

  return (
    <div className="overflow-x-auto bg-surface-strong">
      <table className="w-full min-w-[1120px] border-collapse text-left text-sm">
        <thead className="border-b border-border bg-surface-subtle/80 text-[11px] font-bold uppercase tracking-[0.08em] text-muted">
          <tr>
            <th scope="col" className="w-[29%] px-4 py-2.5">Ürün / varyant</th>
            <th scope="col" className="px-3 py-2.5">Hareket</th>
            <th scope="col" className="px-3 py-2.5 text-right">Miktar</th>
            <th scope="col" className="px-3 py-2.5 text-right">Önce → sonra</th>
            <th scope="col" className="w-[25%] px-3 py-2.5">Açıklama / kaynak</th>
            <th scope="col" className="px-3 py-2.5">Tarih</th>
            <th scope="col" aria-label="Mutabakat" className="w-12 px-3 py-2.5" />
          </tr>
        </thead>
        <tbody className="divide-y divide-border/80">
          {page.items.map((movement) => {
            const balanceHref = buildStockMovementListHref({ ...query, balanceVariantId: movement.productVariantId });
            const productTitle = movement.productTitle?.trim() || "Ürün bilgisi API yanıtında yok";
            const variantLabel = [movement.variantName, movement.variantValue].filter(Boolean).join(": ");
            const sku = movement.sku?.trim() || `Varyant: ${shortId(movement.productVariantId)}`;
            return (
              <tr key={movement.id} className="bg-surface-strong align-middle hover:bg-primary-soft/25">
                <td className="px-4 py-3">
                  <p className="max-w-sm truncate font-semibold text-foreground" title={productTitle}>{productTitle}</p>
                  <p className="mt-1 text-xs text-muted">{variantLabel || "Varyant ayrıntısı API yanıtında yok"}</p>
                  <p className="mt-1 font-mono text-xs text-foreground/75">{sku}</p>
                </td>
                <td className="px-3 py-3">
                  <p className="font-medium text-foreground">{stockMovementTypeLabel(movement.type)}</p>
                  <p className={`mt-1 text-xs font-semibold ${movement.direction === 1 ? "text-success" : "text-danger"}`}>{stockMovementDirectionLabel(movement.direction)}</p>
                </td>
                <td className={`px-3 py-3 text-right font-semibold tabular-nums ${movement.quantityDelta > 0 ? "text-success" : "text-danger"}`}>{movement.quantityDelta > 0 ? "+" : ""}{movement.quantityDelta}</td>
                <td className="px-3 py-3 text-right tabular-nums text-foreground"><span className="font-medium">{movement.stockBeforeMovement}</span><span className="mx-1.5 text-muted">→</span><span className="font-semibold">{movement.stockAfterMovement}</span></td>
                <td className="px-3 py-3">
                  <p className="max-w-xs truncate text-sm text-foreground" title={movement.reason || undefined}>{movement.reason || "Açıklama yok"}</p>
                  {movement.orderId ? <Link href={`/orders/${encodeURIComponent(movement.orderId)}`} className="mt-1 inline-flex text-xs font-semibold text-primary hover:text-primary-hover">Siparişi aç</Link> : movement.returnRequestId ? <p className="mt-1 font-mono text-xs text-muted">İade: {shortId(movement.returnRequestId)}</p> : <p className="mt-1 text-xs text-muted">Manuel veya sistem kaydı</p>}
                </td>
                <td className="whitespace-nowrap px-3 py-3 text-xs text-muted">{formatDate(movement.createdAt)}</td>
                <td className="px-3 py-3 text-right"><Link href={balanceHref} aria-label={`${sku} varyantının stok mutabakatını görüntüle`} className="inline-flex min-h-9 items-center rounded-lg border border-border-strong bg-surface-strong px-2.5 text-xs font-semibold text-foreground hover:border-primary/35 hover:text-primary focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-focus">Kontrol</Link></td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}

// Burada UTC hareket zamanını yönetim kullanımı için Türkiye saatinde gösteriyorum.
function formatDate(value: string): string {
  return new Intl.DateTimeFormat("tr-TR", { dateStyle: "medium", timeStyle: "short", timeZone: "Europe/Istanbul" }).format(new Date(value));
}

// Burada uzun kaynak kimliğini satır yoğunluğunu bozmadan ayırt edilebilir kılıyorum.
function shortId(value: string): string {
  return `${value.slice(0, 8)}…`;
}
