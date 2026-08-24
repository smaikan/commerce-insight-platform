"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";

import { formatCurrency } from "@/lib/formatting/currency";
import type { AccountOrder, AccountReturnPage, ProductVariant } from "@/modules/account/contracts";
import { canCreateOrderReturnRequest } from "@/modules/orders/lifecycle";
import { createGuestReturn, getGuestOrder, getGuestProductVariants, getGuestReturns } from "@/modules/returns/client";
import { formatAccountDate, returnStatusLabel, returnTypeLabel } from "@/modules/returns/presentation";

type ReadyState = { order: AccountOrder; returns: AccountReturnPage; variants: Record<string, ProductVariant[]> };

// Burada guest session'ın izin verdiği sipariş, talep geçmişi ve replacement seçeneklerini tek self-service görünümünde yönetiyorum.
export function GuestReturnsView({ orderId }: { orderId: string }) {
  const router = useRouter();
  const [data, setData] = useState<ReadyState | null>(null);
  const [error, setError] = useState("");
  const [pending, setPending] = useState(false);
  const [type, setType] = useState<0 | 1>(0);

  useEffect(() => {
    let active = true;
    void Promise.all([getGuestOrder(orderId), getGuestReturns(orderId)]).then(async ([order, returns]) => {
      if (!canCreateOrderReturnRequest(order.status)) {
        if (active) setData({ order, returns, variants: {} });
        return;
      }

      // Burada replacement varyantlarını yalnız gerçekten talep oluşturulabilen siparişlerde okuyarak Shipped bilgi ekranını hafif tutuyorum.
      const productIds = [...new Set(order.items.map((item) => item.productId))];
      const pages = await Promise.all(productIds.map(getGuestProductVariants));
      if (active) setData({ order, returns, variants: Object.fromEntries(productIds.map((id, index) => [id, pages[index].items])) });
    }).catch((reason) => { if (active) setError(reason instanceof Error ? reason.message : "Sipariş erişimi açılamadı."); });
    return () => { active = false; };
  }, [orderId]);

  async function submit(formData: FormData) {
    if (!data) return;
    setPending(true); setError("");
    const items = data.order.items.flatMap((item) => {
      const quantity = Number(formData.get(`quantity:${item.id}`));
      if (!Number.isInteger(quantity) || quantity <= 0) return [];
      const replacement = String(formData.get(`replacement:${item.id}`) || "");
      return [{ orderItemId: item.id, quantity, replacementProductVariantId: type === 1 ? replacement || null : null }];
    });
    if (!items.length) { setError("En az bir ürün için adet seçin."); setPending(false); return; }
    if (type === 1 && items.some((item) => !item.replacementProductVariantId)) { setError("Değişime eklenen her ürün için yeni varyant seçin."); setPending(false); return; }
    try {
      const created = await createGuestReturn(orderId, { type, items, customerNote: String(formData.get("customerNote") || "").trim() || null });
      router.push(`/guest-orders/${orderId}/returns/${created.id}`);
    } catch (reason) { setError(reason instanceof Error ? reason.message : "Talep oluşturulamadı."); setPending(false); }
  }

  if (error && !data) return <AccessError message={error} />;
  if (!data) return <main id="main-content" className="page-shell flex-1 py-16" aria-busy="true"><p className="text-sm text-ink-muted">Sipariş ve talepler yükleniyor…</p></main>;
  const eligibleStatus = canCreateOrderReturnRequest(data.order.status);

  return <main id="main-content" className="page-shell max-w-[64rem] flex-1 py-10 sm:py-14"><header className="border-b border-line pb-6"><p className="text-xs font-bold tracking-[0.14em] text-brand-700 uppercase">Misafir sipariş işlemleri</p><h1 className="mt-3 text-3xl font-black text-brand-950">İade ve değişim</h1><p className="mt-2 text-sm text-ink-muted">#{data.order.orderNumber} numaralı siparişiniz</p></header>
    {eligibleStatus ? <form action={submit} className="mt-7 space-y-5"><fieldset className="border border-line bg-surface p-5"><legend className="px-2 text-sm font-black">Yeni talep</legend><div className="flex gap-5"><label className="flex min-h-11 items-center gap-2 text-sm font-bold"><input type="radio" checked={type === 0} onChange={() => setType(0)} /> İade</label><label className="flex min-h-11 items-center gap-2 text-sm font-bold"><input type="radio" checked={type === 1} onChange={() => setType(1)} /> Değişim</label></div><ul className="mt-4 divide-y divide-line border-t border-line">{data.order.items.map((item) => { const eligible = (data.variants[item.productId] || []).filter((variant) => variant.id !== item.productVariantId && variant.isActive && variant.stock > 0 && variant.netPrice === item.unitPrice); return <li key={item.id} className="grid gap-3 py-4 md:grid-cols-[minmax(0,1fr)_7rem_minmax(12rem,0.8fr)] md:items-end"><div><strong className="text-sm">{item.productTitle}</strong><span className="mt-1 block text-xs text-ink-muted">{item.quantity} adet · {item.variantName && item.variantValue ? `${item.variantName}: ${item.variantValue}` : item.variantSku}</span></div><label className="text-xs font-bold text-ink-muted">Adet<select name={`quantity:${item.id}`} defaultValue="0" className="focus-ring mt-2 min-h-11 w-full border border-line bg-surface px-2 text-sm"><option value="0">Seçme</option>{Array.from({ length: item.quantity }, (_, index) => <option key={index + 1}>{index + 1}</option>)}</select></label>{type === 1 ? <label className="text-xs font-bold text-ink-muted">Yeni varyant<select name={`replacement:${item.id}`} defaultValue="" className="focus-ring mt-2 min-h-11 w-full border border-line bg-surface px-2 text-sm"><option value="">Seçin</option>{eligible.map((variant) => <option key={variant.id} value={variant.id}>{variant.name}: {variant.value}</option>)}</select></label> : <span />}</li>; })}</ul><label className="mt-4 grid gap-2 text-sm font-bold">Talep notu<textarea name="customerNote" rows={3} maxLength={1000} className="focus-ring border border-line p-3 font-normal" /></label>{error ? <p role="alert" className="mt-4 text-sm text-danger">{error}</p> : null}<button disabled={pending} className="focus-ring mt-5 min-h-12 bg-brand-950 px-6 text-sm font-bold text-white disabled:opacity-60">{pending ? "Oluşturuluyor…" : "Talebi oluştur"}</button></fieldset></form> : <p className="mt-7 border border-line bg-surface-subtle p-5 text-sm leading-6 text-ink-muted">{data.order.status === 4 ? "Siparişiniz kargoda. İade veya değişim talebi, teslimat kaydı tamamlandığında bu sayfada açılır." : "Bu sipariş henüz iade veya değişim talebine uygun durumda değil."}</p>}
    <section className="mt-8" aria-labelledby="guest-return-history"><h2 id="guest-return-history" className="text-xl font-black text-ink">Talep geçmişi</h2>{data.returns.items.length ? <ul className="mt-4 divide-y divide-line border border-line bg-surface">{data.returns.items.map((item) => <li key={item.id}><Link href={`/guest-orders/${orderId}/returns/${item.id}`} className="focus-ring grid gap-2 p-4 hover:bg-surface-subtle sm:grid-cols-[1fr_auto_auto]"><span><strong className="block">#{item.returnNumber}</strong><small className="text-ink-muted">{returnTypeLabel(item.type)} · {formatAccountDate(item.createdAt)}</small></span><span className="text-xs font-bold text-brand-700">{returnStatusLabel(item.status)}</span><strong className="text-sm tabular-nums">{formatCurrency(item.refundTotal)}</strong></Link></li>)}</ul> : <p className="mt-3 text-sm text-ink-muted">Bu sipariş için daha önce oluşturulmuş talep yok.</p>}</section>
  </main>;
}

function AccessError({ message }: { message: string }) {
  return <main id="main-content" className="page-shell flex flex-1 items-center justify-center py-16"><section className="max-w-lg border border-line bg-surface p-8 text-center"><h1 className="text-2xl font-black text-ink">Sipariş erişimi gerekli</h1><p className="mt-3 text-sm leading-6 text-ink-muted">{message}</p><Link href="/guest-orders/access" className="focus-ring mt-5 inline-flex min-h-11 items-center bg-brand-950 px-5 text-sm font-bold text-white">Erişim bağlantısı iste</Link></section></main>;
}
