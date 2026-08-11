"use client";

import Link from "next/link";
import { useEffect, useState } from "react";

import {
  confirmationProblemMessage,
  loadGuestOrder,
} from "@/modules/checkout/client/checkout-api";
import type { GuestOrder } from "@/modules/checkout/types";

type ConfirmationState =
  | { kind: "loading" }
  | { kind: "ready"; order: GuestOrder }
  | { kind: "error"; message: string };

// Burada guest session grant'iyle alınan authoritative sipariş sonucunu yenilemede de kalıcı bir confirmation ekranında gösteriyorum.
export function OrderConfirmation({ orderId, currency }: { orderId: string; currency: string }) {
  const [state, setState] = useState<ConfirmationState>({ kind: "loading" });

  useEffect(() => {
    let active = true;
    void loadGuestOrder(orderId)
      .then((order) => {
        if (active) setState({ kind: "ready", order });
      })
      .catch((error) => {
        if (active) setState({ kind: "error", message: confirmationProblemMessage(error) });
      });
    return () => {
      active = false;
    };
  }, [orderId]);

  if (state.kind === "loading") return <ConfirmationLoadingState />;

  if (state.kind === "error") {
    return (
      <main id="main-content" className="page-shell flex flex-1 items-center justify-center py-16 sm:py-24">
        <section className="w-full max-w-xl rounded-2xl border border-line bg-surface px-6 py-10 text-center shadow-panel sm:px-10">
          <h1 className="text-2xl font-semibold tracking-[-0.03em] text-ink">Sipariş bilgisi açılamadı</h1>
          <p className="mt-3 text-sm leading-6 text-ink-muted">{state.message}</p>
          <p className="mt-2 text-xs leading-5 text-ink-muted">Sipariş erişim bağlantısı ayrıca sipariş sırasında verdiğiniz e-posta adresine gönderilir.</p>
          <Link href="/products" className="focus-ring mt-6 inline-flex min-h-12 items-center justify-center rounded-lg bg-brand-700 px-6 text-sm font-bold text-white hover:bg-brand-950">Mağazaya dön</Link>
        </section>
      </main>
    );
  }

  const order = state.order;
  return (
    <main id="main-content" className="page-shell max-w-[64rem] flex-1 py-10 sm:py-14 lg:py-16">
      <header className="rounded-2xl border border-line bg-surface px-5 py-8 text-center shadow-panel sm:px-8 sm:py-10">
        <span className="mx-auto inline-flex size-12 items-center justify-center rounded-full bg-surface-subtle text-brand-700" aria-hidden="true">
          <svg viewBox="0 0 24 24" className="size-6" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round"><path d="m5 12 4 4L19 6" /></svg>
        </span>
        <p className="mt-5 text-xs font-bold tracking-[0.14em] text-brand-700 uppercase">Sipariş kaydı oluşturuldu</p>
        <h1 className="mt-2 text-3xl font-semibold tracking-[-0.04em] text-ink sm:text-4xl">Teşekkür ederiz</h1>
        <p className="mt-3 text-sm text-ink-muted">Sipariş numaranız</p>
        <p className="mt-1 break-all text-lg font-bold text-brand-950">{order.orderNumber}</p>
      </header>

      <div className="mt-7 grid gap-6 lg:grid-cols-[minmax(0,1fr)_19rem]">
        <section className="overflow-hidden rounded-2xl border border-line bg-surface" aria-labelledby="confirmation-items-title">
          <div className="border-b border-line px-5 py-4 sm:px-6"><h2 id="confirmation-items-title" className="text-lg font-bold text-ink">Sipariş ürünleri</h2></div>
          <ul className="divide-y divide-line">
            {order.items.map((item) => (
              <li key={item.id} className="flex items-start justify-between gap-4 px-5 py-4 text-sm sm:px-6">
                <span className="min-w-0"><span className="block font-bold text-ink">{item.productTitle}</span><span className="mt-1 block text-xs text-ink-muted">{item.variantSku} · {item.quantity} adet</span></span>
                <span className="shrink-0 font-bold tabular-nums text-ink">{formatMoney(item.totalPrice, currency)}</span>
              </li>
            ))}
          </ul>
        </section>

        <aside className="space-y-6">
          <section className="rounded-2xl border border-line bg-surface p-5" aria-labelledby="confirmation-total-title">
            <h2 id="confirmation-total-title" className="text-base font-bold text-ink">Toplamlar</h2>
            <dl className="mt-4 space-y-3 text-sm">
              <div className="flex justify-between gap-3 text-ink-muted"><dt>Ara toplam</dt><dd className="font-semibold text-ink">{formatMoney(order.subTotal, currency)}</dd></div>
              <div className="flex justify-between gap-3 text-ink-muted"><dt>İndirim</dt><dd className="font-semibold text-ink">{order.discountTotal > 0 ? `−${formatMoney(order.discountTotal, currency)}` : formatMoney(0, currency)}</dd></div>
              <div className="flex justify-between gap-3 text-ink-muted"><dt>Kargo</dt><dd className="font-semibold text-ink">{formatMoney(order.shippingTotal, currency)}</dd></div>
              <div className="flex justify-between gap-3 text-ink-muted"><dt>Vergi</dt><dd className="font-semibold text-ink">{formatMoney(order.taxTotal, currency)}</dd></div>
              <div className="flex justify-between gap-3 border-t border-line pt-3"><dt className="font-bold text-ink">Genel toplam</dt><dd className="font-bold text-brand-950">{formatMoney(order.grandTotal, currency)}</dd></div>
            </dl>
          </section>

          {order.shippingAddress ? (
            <section className="rounded-2xl border border-line bg-surface p-5" aria-labelledby="confirmation-address-title">
              <h2 id="confirmation-address-title" className="text-base font-bold text-ink">Teslimat</h2>
              <address className="mt-3 text-sm not-italic leading-6 text-ink-muted">
                <span className="block font-semibold text-ink">{order.shippingAddress.firstName} {order.shippingAddress.lastName}</span>
                <span className="block">{order.shippingAddress.fullAddress}</span>
                <span className="block">{order.shippingAddress.district} / {order.shippingAddress.city}</span>
                {order.shippingAddress.postalCode ? <span className="block">{order.shippingAddress.postalCode}</span> : null}
              </address>
              {order.shippingMethodName ? <p className="mt-3 border-t border-line pt-3 text-xs font-semibold text-ink-muted">{order.shippingMethodName}</p> : null}
            </section>
          ) : null}
        </aside>
      </div>

      <section className="mt-7 rounded-xl border border-line bg-surface-subtle px-5 py-4 text-sm leading-6 text-ink">
        Siparişiniz oluşturuldu. Ödeme gerektiren siparişlerde güvenli ödeme adımı sonraki aşamada bağlanacak.
      </section>
      <div className="mt-7 text-center"><Link href="/products" className="focus-ring inline-flex min-h-12 items-center justify-center rounded-lg border border-brand-700 px-6 text-sm font-bold text-brand-700 hover:bg-surface-subtle">Alışverişe devam et</Link></div>
    </main>
  );
}

export function ConfirmationLoadingState() {
  return (
    <main id="main-content" className="page-shell max-w-[64rem] flex-1 py-10 sm:py-14" aria-label="Sipariş bilgisi yükleniyor" aria-busy="true">
      <div className="h-64 rounded-2xl border border-line bg-surface" />
      <div className="mt-7 grid gap-6 lg:grid-cols-[minmax(0,1fr)_19rem]"><div className="h-64 rounded-2xl border border-line bg-surface" /><div className="h-64 rounded-2xl border border-line bg-surface" /></div>
    </main>
  );
}

function formatMoney(value: number, currency: string): string {
  return new Intl.NumberFormat("tr-TR", { style: "currency", currency, minimumFractionDigits: 2 }).format(value);
}
