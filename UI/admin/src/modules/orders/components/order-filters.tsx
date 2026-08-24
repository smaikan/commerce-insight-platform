import Link from "next/link";
import { orderStatusOptions } from "@/modules/orders/presentation";
import { hasOrderFilters } from "@/modules/orders/query";
import type { OrderListQuery } from "@/modules/orders/types";

// Burada sipariş filtre kontrollerinin ürün listesiyle aynı yoğunluk ve odak görünümünü kullanmasını sağlıyorum.
const controlClass = "min-h-10 w-full rounded-lg border border-border-strong bg-surface-strong px-3 text-sm text-foreground outline-none focus:border-primary sm:min-h-9";

// Burada Order API'nin desteklediği durum, UTC tarih aralığı ve sayfa boyutu filtrelerini tek araç çubuğunda sunuyorum.
export function OrderFilters({ query }: { query: OrderListQuery }) {
  return (
    <form action="/orders" method="get" className="border-b border-border bg-surface-subtle/60 p-4 sm:p-5">
      <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-[minmax(14rem,1fr)_minmax(12rem,0.8fr)_repeat(2,minmax(11rem,0.7fr))_minmax(12rem,0.75fr)_auto_auto]">
        <label>
          <span className="mb-1.5 block text-xs font-semibold text-muted">Sipariş veya müşteri ara</span>
          <input name="search" type="search" defaultValue={query.search ?? ""} placeholder="Sipariş no, ad veya e-posta" className={controlClass} autoComplete="off" />
        </label>
        <label>
          <span className="mb-1.5 block text-xs font-semibold text-muted">Sipariş durumu</span>
          <select name="status" defaultValue={query.status ?? ""} className={controlClass}>
            <option value="">Tüm durumlar</option>
            {orderStatusOptions.map((status) => (
              <option key={status.value} value={status.value}>{status.label}</option>
            ))}
          </select>
        </label>

        <label>
          <span className="mb-1.5 block text-xs font-semibold text-muted">Başlangıç tarihi</span>
          <input name="createdFrom" type="date" defaultValue={query.createdFrom} className={controlClass} aria-invalid={Boolean(query.dateError)} aria-describedby={query.dateError ? "order-date-error" : "order-date-help"} />
        </label>

        <label>
          <span className="mb-1.5 block text-xs font-semibold text-muted">Bitiş tarihi</span>
          <input name="createdTo" type="date" defaultValue={query.createdTo} className={controlClass} aria-invalid={Boolean(query.dateError)} aria-describedby={query.dateError ? "order-date-error" : "order-date-help"} />
        </label>

        <label>
          <span className="mb-1.5 block text-xs font-semibold text-muted">Sayfa boyutu</span>
          <select name="pageSize" defaultValue={query.pageSize} className={controlClass}>
            {[10, 20, 50, 100].map((size) => (
              <option key={size} value={size}>{size} sipariş / sayfa</option>
            ))}
          </select>
        </label>

        <button type="submit" className="min-h-10 cursor-pointer self-end rounded-lg bg-primary px-4 text-sm font-semibold text-white transition-colors hover:bg-primary-hover sm:min-h-9">
          Uygula
        </button>

        {hasOrderFilters(query) ? (
          <Link href="/orders" className="inline-flex min-h-10 cursor-pointer self-end items-center justify-center rounded-lg border border-border-strong bg-surface-strong px-4 text-sm font-medium text-foreground transition-colors hover:bg-surface-subtle sm:min-h-9">
            Temizle
          </Link>
        ) : null}
      </div>

      <p id="order-date-help" className="mt-3 text-xs leading-5 text-muted">Siparişleri oluşturulma tarihine göre filtreleyin.</p>
      {query.dateError ? <p id="order-date-error" className="mt-2 text-sm font-semibold text-danger" role="alert">{query.dateError}</p> : null}
    </form>
  );
}
