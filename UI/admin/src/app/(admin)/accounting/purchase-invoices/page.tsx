import type { Metadata } from "next";
import Link from "next/link";
import { redirect } from "next/navigation";
import { ApiError } from "@/lib/api/problem";
import { requireAdminPageSession } from "@/lib/auth/session";
import { PageHeader } from "@/modules/admin-shell/components/page-header";
import { AccountingLoadProblem } from "@/modules/accounting/core/components/accounting-load-problem";
import { getPurchaseInvoices } from "@/modules/accounting/purchases/api";
import { PurchaseInvoicePagination } from "@/modules/accounting/purchases/components/purchase-invoice-pagination";
import { PurchaseInvoiceTable } from "@/modules/accounting/purchases/components/purchase-invoice-table";
import { buildPurchaseInvoiceListHref, canonicalPageNumber, parsePurchaseInvoiceListQuery } from "@/modules/accounting/purchases/query";

export const metadata: Metadata = { title: "Alış Faturaları" };

export default async function PurchaseInvoicesPage({ searchParams }: { searchParams: Promise<Record<string, string | string[] | undefined>> }) {
  const query = parsePurchaseInvoiceListQuery(await searchParams);
  const session = await requireAdminPageSession(buildPurchaseInvoiceListHref(query));
  let page;
  try { page = await getPurchaseInvoices(query, session); }
  catch (error) { if (error instanceof ApiError) return <AccountingLoadProblem problem={error.problem} retryHref={buildPurchaseInvoiceListHref(query)} />; throw error; }
  const canonical = canonicalPageNumber(query.pageNumber, page.totalPages);
  if (canonical) redirect(buildPurchaseInvoiceListHref(query, canonical));
  return <div className="mx-auto w-full max-w-screen-2xl"><PageHeader title="Alış Faturaları" description="Tedarikçi belgelerini taslaktan muhasebeleştirmeye kadar izleyin; fiziksel stok hareketi bu modülden oluşturulmaz." backHref="/accounting" actions={<Link href="/accounting/purchase-invoices/new" className="inline-flex min-h-10 items-center rounded-lg bg-primary px-3.5 text-sm font-semibold text-white hover:bg-primary-hover">Alış faturası oluştur</Link>} /><p className="mb-4 rounded-lg border border-border bg-surface-subtle/60 px-3 py-2 text-xs text-muted">Sicil API sözleşmesi gereği yalnız sayfalanabilir; arama, durum filtresi ve serbest sıralama desteklenmiyor.</p>{page.items.length ? <p className="mb-2 text-xs font-medium text-muted sm:hidden">Tüm belge kolonları için tabloyu yatay kaydırın.</p> : null}<section aria-label="Alış faturası listesi" className="overflow-hidden rounded-xl border border-border bg-surface"><PurchaseInvoiceTable page={page} /><PurchaseInvoicePagination page={page} query={query} /></section></div>;
}
