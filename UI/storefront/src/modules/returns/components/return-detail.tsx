import Link from "next/link";

import { formatCurrency } from "@/lib/formatting/currency";
import type { AccountReturn } from "@/modules/account/contracts";
import { formatAccountDateTime, returnStatusLabel, returnTypeLabel } from "@/modules/returns/presentation";

// Burada üye ve misafir akışlarının aynı iade aggregate detayını ortak, erişilebilir bir görünümde sunuyorum.
export function ReturnDetail({ value, backHref }: { value: AccountReturn; backHref: string }) {
  return (
    <article>
      <Link href={backHref} className="focus-ring inline-flex min-h-10 items-center text-sm font-bold text-brand-700 underline-offset-4 hover:underline">← Taleplere dön</Link>
      <header className="mt-3 border-b border-line pb-6">
        <p className="text-xs font-bold tracking-[0.14em] text-brand-700 uppercase">{returnTypeLabel(value.type)} talebi</p>
        <div className="mt-3 flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
          <div><h1 className="break-all text-2xl font-black text-brand-950 sm:text-3xl">#{value.returnNumber}</h1><p className="mt-2 text-sm text-ink-muted">{formatAccountDateTime(value.createdAt)}</p></div>
          <span className="inline-flex min-h-8 w-fit items-center border border-brand-600/20 bg-surface-subtle px-3 text-xs font-bold text-brand-700">{returnStatusLabel(value.status)}</span>
        </div>
      </header>

      <div className="mt-6 grid gap-6 lg:grid-cols-[minmax(0,1fr)_18rem]">
        <section className="border border-line bg-surface" aria-labelledby="return-items-title">
          <div className="border-b border-line px-5 py-4"><h2 id="return-items-title" className="font-black text-ink">Talep ürünleri</h2></div>
          <ul className="divide-y divide-line">{value.items.map((item) => <li key={item.id} className="flex justify-between gap-4 px-5 py-4 text-sm"><span><strong className="block text-ink">{item.productTitle}</strong><span className="mt-1 block text-xs text-ink-muted">SKU: {item.variantSku} · {item.quantity} adet</span>{item.replacementProductVariantId ? <span className="mt-1 block text-xs text-brand-700">Yeni varyant seçildi</span> : null}</span><strong className="shrink-0 tabular-nums text-ink">{formatCurrency(item.refundTotal)}</strong></li>)}</ul>
        </section>
        <aside className="space-y-4">
          <section className="border border-line bg-surface p-5"><h2 className="font-black text-ink">Talep özeti</h2><dl className="mt-4 space-y-3 text-sm"><div className="flex justify-between gap-3"><dt className="text-ink-muted">Tür</dt><dd className="font-bold">{returnTypeLabel(value.type)}</dd></div><div className="flex justify-between gap-3"><dt className="text-ink-muted">Tutar</dt><dd className="font-black tabular-nums">{formatCurrency(value.refundTotal)}</dd></div></dl></section>
          {value.customerNote ? <Note title="Notunuz" value={value.customerNote} /> : null}
          {value.decisionNote ? <Note title="Değerlendirme notu" value={value.decisionNote} /> : null}
        </aside>
      </div>
    </article>
  );
}

function Note({ title, value }: { title: string; value: string }) {
  return <section className="border border-line bg-surface p-5"><h2 className="text-sm font-black text-ink">{title}</h2><p className="mt-2 whitespace-pre-wrap text-sm leading-6 text-ink-muted">{value}</p></section>;
}
