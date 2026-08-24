import type { Metadata } from "next";
import Link from "next/link";
import { redirect } from "next/navigation";
import { ApiError } from "@/lib/api/problem";
import { requireAdminPageSession } from "@/lib/auth/session";
import { PageHeader } from "@/modules/admin-shell/components/page-header";
import { AccountingLoadProblem } from "@/modules/accounting/core/components/accounting-load-problem";
import { getSalesInvoices } from "@/modules/accounting/sales/api";
import { SalesInvoiceRegister } from "@/modules/accounting/sales/components/sales-registers";
import { buildSalesInvoiceListHref, canonicalSalesPage, parseSalesListQuery } from "@/modules/accounting/sales/query";

export const metadata: Metadata = { title: "Satış Faturaları" };
export default async function SalesInvoicesPage({ searchParams }: { searchParams: Promise<Record<string, string | string[] | undefined>> }) { const query = parseSalesListQuery(await searchParams); const session = await requireAdminPageSession(buildSalesInvoiceListHref(query)); let page; try { page = await getSalesInvoices(query, session); } catch (error) { if (error instanceof ApiError) return <AccountingLoadProblem problem={error.problem} retryHref={buildSalesInvoiceListHref(query)} />; throw error; } const canonical = canonicalSalesPage(query.pageNumber, page.totalPages); if (canonical) redirect(buildSalesInvoiceListHref(query, canonical)); return <div className="mx-auto w-full max-w-screen-2xl"><PageHeader title="Satış Faturaları" description="Muhasebe satışlarına bağlı iç faturaları izleyin; fatura ikinci stok veya alacak etkisi oluşturmaz." backHref="/accounting" actions={<Link href="/accounting/sales-invoices/new" className="inline-flex min-h-10 items-center rounded-lg bg-primary px-3.5 text-sm font-semibold text-white hover:bg-primary-hover">Doğrudan fatura oluştur</Link>} /><p className="mb-4 rounded-lg border border-border bg-surface-subtle/60 px-3 py-2 text-xs text-muted">Sicil API sözleşmesi yalnız sayfalamayı destekliyor; arama, durum filtresi ve serbest sıralama uygulanmıyor.</p>{page.items.length ? <p className="mb-2 text-xs font-medium text-muted sm:hidden">Tüm belge kolonları için tabloyu yatay kaydırın.</p> : null}<SalesInvoiceRegister page={page} query={query} /></div>; }
