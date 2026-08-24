import type { Metadata } from "next";
import Link from "next/link";
import { notFound } from "next/navigation";
import { ApiError } from "@/lib/api/problem";
import { requireAdminPageSession } from "@/lib/auth/session";
import { PageHeader } from "@/modules/admin-shell/components/page-header";
import { getPurchaseInvoice } from "@/modules/accounting/purchases/api";

export const metadata: Metadata = { title: "Alış Faturası Düzenleme Sınırı" };

// Burada kayıplı GET→PUT round-trip riski çözülene kadar mevcut taslağı sahte bir düzenleme formuna açmıyorum.
export default async function EditPurchaseInvoicePage({ params }: { params: Promise<{ purchaseInvoiceId: string }> }) {
  const { purchaseInvoiceId } = await params;
  const session = await requireAdminPageSession(`/accounting/purchase-invoices/${encodeURIComponent(purchaseInvoiceId)}/edit`);
  let invoice;
  try { invoice = await getPurchaseInvoice(purchaseInvoiceId, session); }
  catch (error) { if (error instanceof ApiError && error.problem.status === 404) notFound(); throw error; }
  return <div className="mx-auto w-full max-w-3xl"><PageHeader title="Taslak düzenleme sözleşmesi bekliyor" description={invoice.invoiceNumber} backHref={`/accounting/purchase-invoices/${encodeURIComponent(invoice.id)}`} backLabel="Fatura detayına dön" /><section className="rounded-xl border border-amber-300 bg-amber-50 p-5 text-amber-950"><h2 className="text-base font-semibold">Kayıpsız düzenleme şu anda garanti edilemiyor</h2><p className="mt-2 text-sm leading-6">API detay DTO’su başlık/satır indirim yapılandırmasını ve satırın fatura indirimi uygunluk değerini geri döndürmüyor. Bu sayfada kaydetme açılırsa görünmeyen alanlar silinebilir. Tahsis ve gider işlemleri belge detayında ayrı intent olarak kullanılabilir.</p><div className="mt-4"><Link href={`/accounting/purchase-invoices/${encodeURIComponent(invoice.id)}`} className="inline-flex min-h-10 items-center rounded-lg bg-primary px-4 text-sm font-semibold text-white hover:bg-primary-hover">Belge detayına dön</Link></div></section></div>;
}
