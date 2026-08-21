import Link from "next/link";

import { formatCurrency } from "@/lib/formatting/currency";
import { AccountPageHeader } from "@/modules/account/components/account-page-header";
import type { AccountOrderPage } from "@/modules/account/contracts";
import { formatAccountDate, ORDER_STATUS_LABELS, orderStatusLabel } from "@/modules/account/presentation";

// Burada müşteri siparişlerini mobil kart ve masaüstü yoğun satır arasında yeniden akan filtreli bir liste olarak sunuyorum.
export function OrdersView({ orders, status }: { orders: AccountOrderPage; status?: number }) {
  return (
    <section>
      <AccountPageHeader eyebrow="Sipariş yönetimi" title="Siparişlerim" description="Siparişlerinizin güncel durumunu görüntüleyin ve ayrıntılarına güvenli biçimde ulaşın." />

      <form className="mt-6 flex flex-col gap-3 border-y border-line py-4 sm:flex-row sm:items-end" method="get">
        <label className="text-xs font-bold text-ink">
          Sipariş durumu
          <select name="status" defaultValue={status === undefined ? "" : String(status)} className="focus-ring mt-2 min-h-11 w-full border border-line bg-surface px-3 text-sm font-normal text-ink sm:w-56">
            <option value="">Tüm siparişler</option>
            {Object.entries(ORDER_STATUS_LABELS).map(([value, label]) => <option key={value} value={value}>{label}</option>)}
          </select>
        </label>
        <button type="submit" className="focus-ring min-h-11 bg-brand-950 px-5 text-sm font-bold text-white hover:bg-brand-700">Filtrele</button>
        {status !== undefined ? <Link href="/account/orders" className="focus-ring inline-flex min-h-11 items-center justify-center px-3 text-sm font-bold text-brand-700 underline-offset-4 hover:underline">Filtreyi temizle</Link> : null}
      </form>

      {orders.items.length ? (
        <div className="mt-6 border border-line bg-surface">
          <div className="hidden grid-cols-[minmax(0,1.2fr)_minmax(8rem,0.8fr)_minmax(7rem,0.7fr)_minmax(8rem,0.7fr)_auto] gap-4 border-b border-line bg-surface-subtle px-5 py-3 text-[0.6875rem] font-bold tracking-[0.08em] text-ink-muted uppercase md:grid">
            <span>Sipariş</span><span>Tarih</span><span>Durum</span><span className="text-right">Toplam</span><span className="sr-only">Detay</span>
          </div>
          <ul className="divide-y divide-line">
            {orders.items.map((order) => (
              <li key={order.id}>
                <Link href={`/account/orders/${order.id}`} className="focus-ring group grid gap-3 px-5 py-5 hover:bg-surface-subtle md:grid-cols-[minmax(0,1.2fr)_minmax(8rem,0.8fr)_minmax(7rem,0.7fr)_minmax(8rem,0.7fr)_auto] md:items-center md:gap-4 md:py-4">
                  <span><span className="block text-sm font-black text-ink group-hover:text-brand-700">#{order.orderNumber}</span><span className="mt-1 block text-xs text-ink-muted">{order.itemCount} ürün</span></span>
                  <span className="text-xs text-ink-muted md:text-sm">{formatAccountDate(order.createdAt)}</span>
                  <span><OrderStatus status={order.status} /></span>
                  <span className="text-sm font-black tabular-nums text-ink md:text-right">{formatCurrency(order.grandTotal)}</span>
                  <span aria-hidden="true" className="text-brand-700">→</span>
                </Link>
              </li>
            ))}
          </ul>
        </div>
      ) : (
        <div className="mt-6 border border-line bg-surface px-6 py-10 text-center">
          <h2 className="text-lg font-black text-ink">Bu filtrede sipariş bulunamadı</h2>
          <p className="mt-2 text-sm leading-6 text-ink-muted">Farklı bir durum seçebilir veya alışverişe devam edebilirsiniz.</p>
          <Link href={status === undefined ? "/products" : "/account/orders"} className="focus-ring mt-5 inline-flex min-h-11 items-center border border-brand-700 px-4 text-sm font-bold text-brand-700 hover:bg-surface-subtle">{status === undefined ? "Ürünleri keşfet" : "Tüm siparişleri gör"}</Link>
        </div>
      )}

      {orders.totalPages > 1 ? <OrderPagination current={orders.pageNumber} total={orders.totalPages} status={status} /> : null}
    </section>
  );
}

// Burada sipariş durumunu metin ve kontrollü semantik tonla birlikte gösteriyorum.
export function OrderStatus({ status }: { status: number }) {
  const tones: Record<number, string> = {
    0: "border-yellow-300 bg-yellow-100 text-yellow-800",
    1: "border-blue-200 bg-blue-50 text-blue-800",
    2: "border-success/25 bg-success/5 text-success",
    3: "border-amber-200 bg-amber-50 text-amber-800",
    4: "border-indigo-200 bg-indigo-50 text-indigo-800",
    5: "border-success/50 bg-success/10 text-success",
    6: "border-danger/25 bg-danger/5 text-danger",
    7: "border-rose-200 bg-rose-50 text-rose-800",
    8: "border-orange-200 bg-orange-50 text-orange-800",
    9: "border-teal-200 bg-teal-50 text-teal-800",
  };
  const tone = tones[status] || "border-brand-600/20 bg-surface-subtle text-brand-700";
  return <span className={`inline-flex min-h-7 items-center border px-2 text-xs font-bold ${tone}`}>{orderStatusLabel(status)}</span>;
}

// Burada hesap sipariş pagination bağlantılarında seçili durum filtresini koruyorum.
function OrderPagination({ current, total, status }: { current: number; total: number; status?: number }) {
  const href = (page: number) => `/account/orders?${new URLSearchParams({ page: String(page), ...(status === undefined ? {} : { status: String(status) }) })}`;
  return (
    <nav className="mt-6 flex items-center justify-between gap-4 border-t border-line pt-5" aria-label="Sipariş sayfaları">
      {current > 1 ? <Link href={href(current - 1)} className="focus-ring inline-flex min-h-11 items-center border border-line px-4 text-sm font-bold text-ink hover:bg-surface">← Önceki</Link> : <span />}
      <span className="text-xs font-bold text-ink-muted">{current} / {total}</span>
      {current < total ? <Link href={href(current + 1)} className="focus-ring inline-flex min-h-11 items-center border border-line px-4 text-sm font-bold text-ink hover:bg-surface">Sonraki →</Link> : <span />}
    </nav>
  );
}
