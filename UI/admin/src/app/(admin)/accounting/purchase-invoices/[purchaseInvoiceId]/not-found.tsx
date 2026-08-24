import Link from "next/link";

export default function PurchaseInvoiceNotFound() {
  return <div className="mx-auto max-w-xl rounded-xl border border-border bg-surface p-6 text-center"><h1 className="text-lg font-semibold">Alış faturası bulunamadı</h1><p className="mt-2 text-sm text-muted">Belge silinmiş, taşınmış veya erişim kapsamınız dışında olabilir.</p><Link href="/accounting/purchase-invoices" className="mt-4 inline-flex min-h-10 items-center rounded-lg bg-primary px-4 text-sm font-semibold text-white">Alış faturalarına dön</Link></div>;
}
