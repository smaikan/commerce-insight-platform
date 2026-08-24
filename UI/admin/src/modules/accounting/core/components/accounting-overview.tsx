import Link from "next/link";
import type { AccountingQueue } from "@/modules/accounting/core/types";

export function AccountingOverview({ queues }: { queues: AccountingQueue[] }) {
  return (
    <div className="space-y-5">
      <section className="grid gap-3 md:grid-cols-2 xl:grid-cols-4" aria-label="Muhasebe operasyon kuyrukları">
        {queues.map((queue) => (
          <Link key={queue.key} href={queue.href} className="group rounded-xl border border-border bg-surface p-4 hover:border-primary/50 hover:bg-primary-soft/10">
            <div className="flex items-start justify-between gap-3">
              <div><h2 className="font-semibold">{queue.title}</h2><p className="mt-1 text-sm leading-5 text-muted">{queue.description}</p></div>
              <span className="text-2xl font-semibold tabular-nums group-hover:text-primary">{queue.unavailable ? "—" : queue.totalCount}</span>
            </div>
            {queue.unavailable ? <p className="mt-4 text-xs font-medium text-warning">Veri şu anda alınamıyor.</p> : <span className="mt-4 block text-xs text-muted">Kayıt sayısı API totalCount değeridir.</span>}
          </Link>
        ))}
      </section>

      <section className="overflow-hidden rounded-xl border border-border bg-surface">
        <div className="border-b border-border px-4 py-4"><h2 className="font-semibold">Çalışma alanları</h2><p className="mt-1 text-sm text-muted">Ön muhasebe kayıtlarını iş akışına göre yönetin.</p></div>
        <Link href="/accounting/current-accounts" className="flex min-h-16 items-center justify-between gap-4 px-4 py-3 hover:bg-primary-soft/20">
          <span><strong className="block">Cari hesaplar</strong><span className="mt-1 block text-sm text-muted">Müşteri ve tedarikçi master kayıtları ile ekstreler.</span></span><span aria-hidden="true" className="text-primary">→</span>
        </Link>
        <Link href="/accounting/purchase-invoices" className="flex min-h-16 items-center justify-between gap-4 border-t border-border px-4 py-3 hover:bg-primary-soft/20"><span><strong className="block">Alış faturaları</strong><span className="mt-1 block text-sm text-muted">Tedarikçi belgeleri, stok tahsisleri ve FIFO maliyet etkileri.</span></span><span aria-hidden="true" className="text-primary">→</span></Link>
        <Link href="/accounting/sales-orders" className="flex min-h-16 items-center justify-between gap-4 border-t border-border px-4 py-3 hover:bg-primary-soft/20"><span><strong className="block">Muhasebe satışları</strong><span className="mt-1 block text-sm text-muted">Müşteri cari satışları, stok çıkışı, FIFO ve alacak yaşam döngüsü.</span></span><span aria-hidden="true" className="text-primary">→</span></Link>
        <Link href="/accounting/sales-invoices" className="flex min-h-16 items-center justify-between gap-4 border-t border-border px-4 py-3 hover:bg-primary-soft/20"><span><strong className="block">Satış faturaları</strong><span className="mt-1 block text-sm text-muted">Muhasebe satışlarına bağlı iç fatura sicili ve belge akışı.</span></span><span aria-hidden="true" className="text-primary">→</span></Link>
        <Link href="/accounting/payments" className="flex min-h-16 items-center justify-between gap-4 border-t border-border px-4 py-3 hover:bg-primary-soft/20"><span><strong className="block">Ödemeler ve tahsilatlar</strong><span className="mt-1 block text-sm text-muted">Cari açık kalem dağıtımları, tedarikçi avansları ve iptal denetimi.</span></span><span aria-hidden="true" className="text-primary">→</span></Link>
        <Link href="/accounting/treasury" className="flex min-h-16 items-center justify-between gap-4 border-t border-border px-4 py-3 hover:bg-primary-soft/20"><span><strong className="block">Kasa ve banka</strong><span className="mt-1 block text-sm text-muted">Türetilmiş bakiyeler, ekstreler, manuel hareketler ve atomik transferler.</span></span><span aria-hidden="true" className="text-primary">→</span></Link>
        <Link href="/accounting/expenses" className="flex min-h-16 items-center justify-between gap-4 border-t border-border px-4 py-3 hover:bg-primary-soft/20"><span><strong className="block">Giderler</strong><span className="mt-1 block text-sm text-muted">Genel gider defteri ve gider kategorileri.</span></span><span aria-hidden="true" className="text-primary">→</span></Link>
        <Link href="/accounting/costing" className="flex min-h-16 items-center justify-between gap-4 border-t border-border px-4 py-3 hover:bg-primary-soft/20"><span><strong className="block">FIFO maliyet yönetimi</strong><span className="mt-1 block text-sm text-muted">Açılış stok maliyeti düzeltmeleri ve varyant maliyet denetim izi.</span></span><span aria-hidden="true" className="text-primary">→</span></Link>
        <Link href="/accounting/reports" className="flex min-h-16 items-center justify-between gap-4 border-t border-border px-4 py-3 hover:bg-primary-soft/20"><span><strong className="block">Muhasebe raporları</strong><span className="mt-1 block text-sm text-muted">Belge, FIFO, kârlılık, cari, nakit ve KDV rapor dizini.</span></span><span aria-hidden="true" className="text-primary">→</span></Link>
      </section>
    </div>
  );
}
