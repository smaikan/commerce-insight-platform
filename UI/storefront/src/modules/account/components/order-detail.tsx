import Link from "next/link";

import { formatCurrency } from "@/lib/formatting/currency";
import { CancelOrderControl } from "@/modules/account/components/cancel-order-control";
import { OrderItemMedia } from "@/modules/account/components/order-item-media";
import { OrderStatus } from "@/modules/account/components/orders-view";
import type { AccountOrder } from "@/modules/account/contracts";
import { formatAccountDate, formatAccountDateTime, safeTrackingUrl } from "@/modules/account/presentation";

// Burada sipariş snapshot'larını ürün, toplam, adres ve gerçek kargo hareketleri hiyerarşisinde ayrıntılı olarak sunuyorum.
export function OrderDetail({ order }: { order: AccountOrder }) {
  const trackingUrl = safeTrackingUrl(order.trackingUrl);
  const canCancel = order.status === 0 || order.status === 1;

  return (
    <article>
      <Link href="/account/orders" className="focus-ring inline-flex min-h-10 items-center text-sm font-bold text-brand-700 underline-offset-4 hover:underline">← Siparişlerime dön</Link>
      <header className="mt-3 border-b border-line pb-6">
        <div className="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
          <div>
            <p className="text-xs font-bold tracking-[0.14em] text-brand-700 uppercase">Sipariş detayı</p>
            <h1 className="mt-3 break-words text-2xl font-black tracking-[-0.025em] text-brand-950 sm:text-3xl">#{order.orderNumber}</h1>
            <p className="mt-2 text-sm text-ink-muted">{formatAccountDate(order.createdAt)} tarihinde oluşturuldu.</p>
          </div>
          <OrderStatus status={order.status} />
        </div>
      </header>

      <div className="mt-7 grid gap-6 xl:grid-cols-[minmax(0,1fr)_20rem]">
        <div className="space-y-6">
          <section className="overflow-hidden border border-line bg-surface" aria-labelledby="order-items-title">
            <div className="border-b border-line px-5 py-4"><h2 id="order-items-title" className="text-base font-black text-ink">Sipariş ürünleri</h2></div>
            <ul className="divide-y divide-line">{order.items.map((item) => <OrderItemMedia key={item.id} item={item} />)}</ul>
          </section>

          <ShippingTracking order={order} trackingUrl={trackingUrl} />

          <div className="grid gap-6 md:grid-cols-2">
            {order.shippingAddress ? <OrderAddress title="Teslimat adresi" address={order.shippingAddress} method={order.shippingMethodName} /> : null}
            {order.billingAddress ? <OrderAddress title="Fatura adresi" address={order.billingAddress} /> : null}
          </div>
        </div>

        <aside className="space-y-6 xl:sticky xl:top-28 xl:self-start">
          <section className="border border-line bg-surface p-5" aria-labelledby="order-totals-title">
            <h2 id="order-totals-title" className="text-base font-black text-ink">Sipariş özeti</h2>
            <dl className="mt-4 space-y-3 text-sm">
              <TotalRow label="Ara toplam" value={order.subTotal} />
              <TotalRow label="İndirim" value={order.discountTotal} negative={order.discountTotal > 0} />
              <TotalRow label="Kargo" value={order.shippingTotal} />
              <TotalRow label="Vergi" value={order.taxTotal} />
              <div className="flex justify-between gap-4 border-t border-line pt-4"><dt className="font-black text-ink">Genel toplam</dt><dd className="font-black tabular-nums text-brand-950">{formatCurrency(order.grandTotal)}</dd></div>
            </dl>
            {order.couponCode ? <p className="mt-4 border-t border-line pt-3 text-xs text-ink-muted">Kupon: <strong className="text-ink">{order.couponCode}</strong></p> : null}
          </section>

          {order.payments.length ? (
            <section className="border border-line bg-surface p-5" aria-labelledby="payment-title">
              <h2 id="payment-title" className="text-base font-black text-ink">Ödeme</h2>
              <ul className="mt-3 space-y-3">{order.payments.map((payment) => <li key={payment.id} className="flex items-start justify-between gap-3 border-t border-line pt-3 first:border-t-0 first:pt-0"><span><span className="block text-xs font-bold text-ink">{paymentStatusLabel(payment.status)}</span><span className="mt-1 block text-xs text-ink-muted">{formatAccountDate(payment.createdAt)}</span></span><span className="text-sm font-black tabular-nums text-ink">{formatCurrency(payment.amount)}</span></li>)}</ul>
            </section>
          ) : null}

          {canCancel ? <CancelOrderControl orderId={order.id} /> : null}
          {order.status === 5 || order.status === 8 || order.status === 9 ? (
            <Link href={`/account/orders/${order.id}/return`} className="focus-ring inline-flex min-h-11 w-full items-center justify-center border border-brand-700 px-4 text-sm font-bold text-brand-700 hover:bg-surface-subtle">
              İade veya değişim talebi oluştur
            </Link>
          ) : null}
        </aside>
      </div>
    </article>
  );
}

// Burada yalnız gerçek kargo alanlarından takip ve teslimat hareketlerini gösterip null değerlerde tahmin üretmiyorum.
function ShippingTracking({ order, trackingUrl }: { order: AccountOrder; trackingUrl: string | null }) {
  const hasTracking = Boolean(order.shippingCarrier && order.trackingNumber);
  const hasMovement = Boolean(order.shippedAt || order.deliveredAt);
  if (!hasTracking && !hasMovement) return null;

  return (
    <section className="border border-line bg-surface" aria-labelledby="shipping-tracking-title">
      <div className="border-b border-line px-5 py-4"><h2 id="shipping-tracking-title" className="text-base font-black text-ink">Kargo takibi</h2></div>
      <div className="grid gap-5 px-5 py-5 sm:grid-cols-2">
        {hasTracking ? (
          <div>
            <p className="text-xs font-bold text-ink-muted">Taşıyıcı</p>
            <p className="mt-1 text-sm font-black text-ink">{order.shippingCarrier}</p>
            <p className="mt-3 text-xs font-bold text-ink-muted">Takip numarası</p>
            <code className="mt-1 block break-all bg-surface-subtle px-3 py-2 text-sm font-bold text-brand-950">{order.trackingNumber}</code>
            {trackingUrl ? <a href={trackingUrl} target="_blank" rel="noreferrer" className="focus-ring mt-3 inline-flex min-h-11 items-center bg-brand-950 px-4 text-sm font-bold text-white hover:bg-brand-700">Kargoyu takip et ↗</a> : null}
          </div>
        ) : null}
        {hasMovement ? (
          <dl className="space-y-4 border-l-0 border-line sm:border-l sm:pl-5">
            {order.shippedAt ? <TrackingDate label="Kargoya verildi" value={order.shippedAt} /> : null}
            {order.deliveredAt ? <TrackingDate label="Teslim edildi" value={order.deliveredAt} /> : null}
          </dl>
        ) : null}
      </div>
    </section>
  );
}

// Burada API'nin gerçek kargo zamanlarını kronolojik okunabilen etiket-değer çifti olarak sunuyorum.
function TrackingDate({ label, value }: { label: string; value: string }) {
  return <div><dt className="text-xs font-bold text-ink-muted">{label}</dt><dd className="mt-1 text-sm font-black text-ink">{formatAccountDateTime(value)}</dd></div>;
}

// Burada sipariş anındaki değişmez adres snapshot'ını kayıtlı adresle karıştırmadan gösteriyorum.
function OrderAddress({ title, address, method }: { title: string; address: NonNullable<AccountOrder["shippingAddress"]>; method?: string | null }) {
  return (
    <section className="border border-line bg-surface p-5" aria-labelledby={`${title.replaceAll(" ", "-")}-title`}>
      <h2 id={`${title.replaceAll(" ", "-")}-title`} className="text-base font-black text-ink">{title}</h2>
      <address className="mt-3 text-sm not-italic leading-6 text-ink-muted">
        <span className="block font-bold text-ink">{address.firstName} {address.lastName}</span>
        <span className="block">{address.fullAddress}</span>
        <span className="block">{address.district} / {address.city}{address.postalCode ? ` · ${address.postalCode}` : ""}</span>
        <span className="mt-1 block">{address.phoneNumber}</span>
      </address>
      {method ? <p className="mt-4 border-t border-line pt-3 text-xs font-semibold text-ink-muted">Teslimat yöntemi: {method}</p> : null}
    </section>
  );
}

// Burada sipariş toplam kalemlerini API tutarlarını değiştirmeden tek biçimde gösteriyorum.
function TotalRow({ label, value, negative = false }: { label: string; value: number; negative?: boolean }) {
  return <div className="flex justify-between gap-4 text-ink-muted"><dt>{label}</dt><dd className="font-semibold tabular-nums text-ink">{negative ? "−" : ""}{formatCurrency(value)}</dd></div>;
}

// Burada ödeme enumunu kullanıcıya anlamlı fakat API durumunu değiştirmeyen etiketlere çeviriyorum.
function paymentStatusLabel(status: number): string {
  return ({ 0: "Ödeme bekliyor", 1: "Ödendi", 2: "Ödeme başarısız", 3: "İade edildi", 4: "Ödeme iptal edildi" } as Record<number, string>)[status] ?? "Ödeme durumu güncelleniyor";
}
