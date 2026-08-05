import Link from "next/link";
import { OrderExpandableRows } from "@/modules/orders/components/order-expandable-rows";
import { formatOrderAmount, formatOrderDate, orderStatusClass, orderStatusLabel } from "@/modules/orders/presentation";
import { hasOrderFilters } from "@/modules/orders/query";
import type { OrderListQuery, OrderPage } from "@/modules/orders/types";

// Burada sipariş özetlerini daha sakin satır yoğunluğu, birleşik tarih bilgisi ve istek üzerine açılan müşteri özetiyle gösteriyorum.
export function OrderTable({ page, query }: { page: OrderPage; query: OrderListQuery }) {
  if (page.items.length === 0) {
    return (
      <div className="px-5 py-14 text-center">
        <h2 className="text-base font-semibold text-foreground">
          {hasOrderFilters(query) ? "Filtrelere uyan sipariş bulunamadı" : "Henüz sipariş bulunmuyor"}
        </h2>
        <p className="mx-auto mt-2 max-w-lg text-sm leading-6 text-muted">
          {hasOrderFilters(query)
            ? "Durum veya tarih aralığını değiştirerek tekrar deneyin."
            : "Yeni siparişler oluştuğunda bu operasyon listesinde görünecek."}
        </p>
      </div>
    );
  }

  return (
    <div className="overflow-x-auto bg-surface-strong">
      <table className="w-full min-w-[920px] border-collapse text-left text-sm">
        <thead className="border-b border-border bg-surface-subtle/80 text-[11px] font-bold uppercase tracking-[0.08em] text-muted">
          <tr>
            <th scope="col" className="w-[32%] px-5 py-3.5">Sipariş</th>
            <th scope="col" className="px-4 py-3.5">Durum</th>
            <th scope="col" className="px-4 py-3.5">İçerik</th>
            <th scope="col" className="px-4 py-3.5 text-right">Toplam</th>
            <th scope="col" className="px-4 py-3.5">Tarihler</th>
            <th scope="col" className="w-32 px-4 py-3.5 text-right">Müşteri özeti</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-border/80">
          {page.items.map((order) => {
            const orderHref = `/orders/${encodeURIComponent(order.id)}`;
            return (
              <OrderExpandableRows key={order.id} orderId={order.id} orderNumber={order.orderNumber} orderHref={orderHref}>
                <td className="px-5 py-3.5">
                  <Link href={orderHref} className="flex min-w-0 items-center gap-3 rounded-lg outline-none focus-visible:ring-2 focus-visible:ring-focus focus-visible:ring-offset-2">
                    <span className="flex size-11 shrink-0 items-center justify-center rounded-xl border border-primary/15 bg-primary-soft/45 text-primary">
                      <svg aria-hidden="true" viewBox="0 0 24 24" className="size-5 fill-none stroke-current stroke-[1.8]">
                        <path d="M7 3.5h10a2 2 0 0 1 2 2v15l-3-2-4 2-4-2-3 2v-15a2 2 0 0 1 2-2Z" strokeLinejoin="round" />
                        <path d="M8.5 8h7M8.5 12h7" strokeLinecap="round" />
                      </svg>
                    </span>
                    <span className="min-w-0">
                      <span className="block truncate text-[15px] font-bold leading-5 text-foreground transition-colors group-hover:text-primary">{order.orderNumber}</span>
                      <span className="mt-1 block max-w-64 truncate text-xs text-muted" title={order.id}>Sipariş kaydı · {order.id.slice(0, 8)}</span>
                    </span>
                  </Link>
                </td>
                <td className="px-4 py-3.5">
                  <span className={`inline-flex rounded-md border px-2 py-1 text-xs font-bold ${orderStatusClass(order.status)}`}>{orderStatusLabel(order.status)}</span>
                </td>
                <td className="px-4 py-3.5">
                  <span className="font-semibold tabular-nums text-foreground">{order.itemCount}</span>
                  <span className="ml-1 text-xs text-muted">kalem</span>
                </td>
                <td className="px-4 py-3.5 text-right">
                  <span className="font-bold tabular-nums text-foreground">{formatOrderAmount(order.grandTotal)}</span>
                </td>
                <td className="whitespace-nowrap px-4 py-3.5">
                  <span className="block font-medium text-foreground">{formatOrderDate(order.createdAt)}</span>
                  <span className="mt-1 block text-xs text-muted">{order.paidAt ? `Ödeme: ${formatOrderDate(order.paidAt)}` : "Ödeme tarihi yok"}</span>
                </td>
              </OrderExpandableRows>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}
