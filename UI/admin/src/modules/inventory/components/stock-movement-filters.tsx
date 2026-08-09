import Link from "next/link";
import { hasStockMovementFilters } from "@/modules/inventory/query";
import { stockMovementDirectionOptions, stockMovementTypeOptions } from "@/modules/inventory/stock-movement-rules";
import type { StockMovementListQuery } from "@/modules/inventory/types";

// Burada stok defteri filtrelerini URL durumunda saklayan kompakt araç çubuğunu sunuyorum.
export function StockMovementFilters({ query }: { query: StockMovementListQuery }) {
  return (
    <form action="/inventory/stock-movements" method="get" className="border-b border-border bg-surface-subtle/40 px-4 py-3 sm:px-5">
      <div className="grid gap-2 md:grid-cols-2 xl:grid-cols-3 2xl:grid-cols-[minmax(15rem,1.55fr)_minmax(10rem,0.9fr)_minmax(11rem,0.9fr)_minmax(10rem,0.8fr)_minmax(10rem,0.8fr)_auto]">
        <div>
          <label className="sr-only" htmlFor="stock-search">Ürün, varyant veya SKU ara</label>
          <input id="stock-search" name="search" maxLength={250} defaultValue={query.search} placeholder="Ürün, varyant veya SKU ara" className="min-h-10 w-full rounded-lg border border-border-strong bg-surface-strong px-3 text-sm text-foreground outline-none placeholder:text-muted focus:border-primary focus:ring-2 focus:ring-focus/30 sm:min-h-9" />
        </div>
        <div>
          <label className="sr-only" htmlFor="stock-direction">Yön</label>
          <select id="stock-direction" name="direction" defaultValue={query.direction ?? ""} className="min-h-10 w-full rounded-lg border border-border-strong bg-surface-strong px-3 text-sm text-foreground outline-none focus:border-primary focus:ring-2 focus:ring-focus/30 sm:min-h-9">
            <option value="">Tüm yönler</option>
            {stockMovementDirectionOptions.map((option) => <option key={option.value} value={option.value}>{option.label}</option>)}
          </select>
        </div>
        <div>
          <label className="sr-only" htmlFor="stock-type">Hareket türü</label>
          <select id="stock-type" name="type" defaultValue={query.type ?? ""} className="min-h-10 w-full rounded-lg border border-border-strong bg-surface-strong px-3 text-sm text-foreground outline-none focus:border-primary focus:ring-2 focus:ring-focus/30 sm:min-h-9">
            <option value="">Tüm hareket türleri</option>
            {stockMovementTypeOptions.map((option) => <option key={option.value} value={option.value}>{option.label}</option>)}
          </select>
        </div>
        <div>
          <label className="sr-only" htmlFor="stock-created-from">Başlangıç tarihi</label>
          <input id="stock-created-from" name="createdFrom" type="date" defaultValue={query.createdFrom} className="min-h-10 w-full rounded-lg border border-border-strong bg-surface-strong px-3 text-sm text-foreground outline-none focus:border-primary focus:ring-2 focus:ring-focus/30 sm:min-h-9" />
        </div>
        <div>
          <label className="sr-only" htmlFor="stock-created-to">Bitiş tarihi</label>
          <input id="stock-created-to" name="createdTo" type="date" defaultValue={query.createdTo} className="min-h-10 w-full rounded-lg border border-border-strong bg-surface-strong px-3 text-sm text-foreground outline-none focus:border-primary focus:ring-2 focus:ring-focus/30 sm:min-h-9" />
        </div>
        <div className="flex gap-2">
          <button type="submit" className="min-h-10 rounded-lg bg-primary px-3 text-sm font-semibold text-white hover:bg-primary-hover focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-focus sm:min-h-9">Uygula</button>
          {hasStockMovementFilters(query) ? <Link href="/inventory/stock-movements" className="inline-flex min-h-10 items-center rounded-lg border border-border-strong bg-surface-strong px-3 text-sm font-semibold text-foreground hover:bg-surface-subtle focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-focus sm:min-h-9">Temizle</Link> : null}
        </div>
      </div>
      <div className="mt-2 flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
        <p className="text-xs text-muted">Arama ürün başlığı, varyant adı/değeri ve SKU üzerinde çalışır.</p>
        <label className="flex items-center gap-2 text-xs font-medium text-muted">
          Sayfa başına
          <select name="pageSize" defaultValue={query.pageSize} className="min-h-8 rounded-md border border-border-strong bg-surface-strong px-2 text-xs text-foreground outline-none focus:border-primary">
            {[20, 50, 100].map((size) => <option key={size} value={size}>{size}</option>)}
          </select>
        </label>
      </div>
      {query.dateError ? <p className="mt-2 text-sm font-medium text-danger" role="alert">{query.dateError}</p> : null}
      {query.productVariantId ? <input type="hidden" name="productVariantId" value={query.productVariantId} /> : null}
      {query.balanceVariantId ? <input type="hidden" name="balanceVariantId" value={query.balanceVariantId} /> : null}
    </form>
  );
}
