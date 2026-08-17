import Link from "next/link";

import { formatCurrency } from "@/lib/formatting/currency";
import type { AccountReturnPage } from "@/modules/account/contracts";
import { AccountPageHeader } from "@/modules/account/components/account-page-header";
import { formatAccountDate, returnStatusLabel, returnTypeLabel } from "@/modules/returns/presentation";

// Burada müşterinin iade geçmişini tür, durum ve tutar bilgileriyle ayrıntıya bağlanan sayfalı bir liste olarak sunuyorum.
export function ReturnsList({ returns, page }: { returns: AccountReturnPage; page: number }) {
  return <section>
    <AccountPageHeader eyebrow="Satış sonrası" title="İade ve değişim" description="İade ve değişim taleplerinizi, değerlendirme durumlarını ve sonuçlarını takip edin." />
    {returns.items.length ? <ul className="mt-6 divide-y divide-line border border-line bg-surface">{returns.items.map((item) => <li key={item.id}><Link href={`/account/returns/${item.id}`} className="focus-ring grid gap-3 px-5 py-5 hover:bg-surface-subtle sm:grid-cols-[minmax(0,1fr)_auto_auto] sm:items-center"><span><strong className="block text-sm text-ink">#{item.returnNumber}</strong><span className="mt-1 block text-xs text-ink-muted">{returnTypeLabel(item.type)} · {item.itemCount} ürün · {formatAccountDate(item.createdAt)}</span></span><span className="text-xs font-bold text-brand-700">{returnStatusLabel(item.status)}</span><strong className="text-sm tabular-nums text-ink">{formatCurrency(item.refundTotal)}</strong></Link></li>)}</ul> : <div className="mt-6 border border-line bg-surface px-6 py-10 text-center"><h2 className="text-lg font-black text-ink">Henüz talebiniz yok</h2><p className="mt-2 text-sm text-ink-muted">Teslim edilen siparişinizin detayından iade veya değişim talebi oluşturabilirsiniz.</p><Link href="/account/orders" className="focus-ring mt-5 inline-flex min-h-11 items-center border border-brand-700 px-4 text-sm font-bold text-brand-700">Siparişlerime git</Link></div>}
    {returns.totalPages > 1 ? <nav aria-label="İade talebi sayfaları" className="mt-6 flex justify-between border-t border-line pt-5">{page > 1 ? <Link className="focus-ring min-h-11 px-4 py-3 text-sm font-bold text-brand-700" href={`/account/returns?page=${page - 1}`}>← Önceki</Link> : <span />}{page < returns.totalPages ? <Link className="focus-ring min-h-11 px-4 py-3 text-sm font-bold text-brand-700" href={`/account/returns?page=${page + 1}`}>Sonraki →</Link> : <span />}</nav> : null}
  </section>;
}
