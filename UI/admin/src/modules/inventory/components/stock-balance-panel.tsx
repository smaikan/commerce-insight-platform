import { buildStockMovementListHref } from "@/modules/inventory/query";
import type { StockBalance, StockMovementListQuery } from "@/modules/inventory/types";

// Burada seçili varyantın kayıtlı stok önbelleği ile defter toplamını ayrı bir mutabakat panelinde gösteriyorum.
export function StockBalancePanel({ balance, query }: { balance?: StockBalance; query: StockMovementListQuery }) {
  return (
    <aside aria-labelledby="stock-balance-title" className="rounded-xl border border-border bg-surface">
      <div className="border-b border-border bg-surface-subtle px-4 py-3">
        <h2 id="stock-balance-title" className="text-base font-semibold text-foreground">Stok mutabakatı</h2>
        <p className="mt-1 text-sm text-muted">Varyant stoğunu hareket defteriyle karşılaştırın.</p>
      </div>
      <form action="/inventory/stock-movements" method="get" className="p-4">
        {query.search ? <input type="hidden" name="search" value={query.search} /> : null}
        {query.productVariantId ? <input type="hidden" name="productVariantId" value={query.productVariantId} /> : null}
        {query.direction ? <input type="hidden" name="direction" value={query.direction} /> : null}
        {query.type ? <input type="hidden" name="type" value={query.type} /> : null}
        {query.createdFrom ? <input type="hidden" name="createdFrom" value={query.createdFrom} /> : null}
        {query.createdTo ? <input type="hidden" name="createdTo" value={query.createdTo} /> : null}
        <label htmlFor="balance-variant-id" className="text-sm font-semibold text-foreground">Varyant kimliği</label>
        <input id="balance-variant-id" name="balanceVariantId" type="text" defaultValue={query.balanceVariantId} placeholder="UUID" className="mt-2 min-h-10 w-full rounded-lg border border-border-strong bg-surface-strong px-3 font-mono text-xs text-foreground outline-none placeholder:font-sans placeholder:text-muted focus:border-primary focus:ring-2 focus:ring-focus/30" />
        <button type="submit" className="mt-3 min-h-10 w-full rounded-lg border border-border-strong bg-surface-strong px-3 text-sm font-semibold text-foreground hover:bg-surface-subtle focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-focus">Mutabakatı kontrol et</button>
      </form>
      {balance ? (
        <div className="border-t border-border px-4 py-4">
          <dl className="grid grid-cols-2 gap-x-3 gap-y-4 text-sm">
            <div><dt className="text-muted">Kayıtlı stok</dt><dd className="mt-1 text-lg font-semibold tabular-nums text-foreground">{balance.persistedStock}</dd></div>
            <div><dt className="text-muted">Hareket toplamı</dt><dd className="mt-1 text-lg font-semibold tabular-nums text-foreground">{balance.movementBalance}</dd></div>
          </dl>
          <p className={`mt-4 rounded-lg border px-3 py-2 text-sm font-semibold ${balance.isConsistent ? "border-success/25 bg-success/10 text-success" : "border-danger/30 bg-danger/10 text-danger"}`}>{balance.isConsistent ? "Kayıtlı stok ile hareket defteri tutarlı." : "Uyumsuzluk tespit edildi. Varyant geçmişini kontrol edin."}</p>
          <a href={buildStockMovementListHref({ ...query, productVariantId: balance.productVariantId, balanceVariantId: undefined, pageNumber: 1 })} className="mt-3 inline-flex text-sm font-semibold text-primary hover:text-primary-hover">Bu varyantın hareketlerini filtrele</a>
        </div>
      ) : <p className="border-t border-border px-4 py-3 text-xs leading-5 text-muted">UUID girildiğinde kayıtlı stok ve tüm hareketlerin toplamı karşılaştırılır.</p>}
    </aside>
  );
}
