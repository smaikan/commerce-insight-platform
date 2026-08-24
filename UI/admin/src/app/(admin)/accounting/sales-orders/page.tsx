import type { Metadata } from "next";
import Link from "next/link";
import { redirect } from "next/navigation";
import { ApiError } from "@/lib/api/problem";
import { requireAdminPageSession } from "@/lib/auth/session";
import { PageHeader } from "@/modules/admin-shell/components/page-header";
import { AccountingLoadProblem } from "@/modules/accounting/core/components/accounting-load-problem";
import { getSalesOrders } from "@/modules/accounting/sales/api";
import { SalesOrderRegister } from "@/modules/accounting/sales/components/sales-registers";
import { buildSalesOrderListHref, canonicalSalesPage, parseSalesListQuery } from "@/modules/accounting/sales/query";

export const metadata: Metadata = { title: "Muhasebe Satışları" };
export default async function SalesOrdersPage({ searchParams }: { searchParams: Promise<Record<string, string | string[] | undefined>> }) {
  const query = parseSalesListQuery(await searchParams); const session = await requireAdminPageSession(buildSalesOrderListHref(query));
  let page; try { page = await getSalesOrders(query, session); } catch (error) { if (error instanceof ApiError) return <AccountingLoadProblem problem={error.problem} retryHref={buildSalesOrderListHref(query)} />; throw error; }
  const canonical = canonicalSalesPage(query.pageNumber, page.totalPages); if (canonical) redirect(buildSalesOrderListHref(query, canonical));
  return <div className="mx-auto w-full max-w-screen-2xl"><PageHeader title="Muhasebe Satışları" description="E-ticaret siparişlerinden ayrı, müşteri carisine bağlı satış belgelerini stok, FIFO ve alacak etkileriyle yönetin." backHref="/accounting" actions={<Link href="/accounting/sales-orders/new" className="inline-flex min-h-10 items-center rounded-lg bg-primary px-3.5 text-sm font-semibold text-white hover:bg-primary-hover">Muhasebe satışı oluştur</Link>} /><p className="mb-4 rounded-lg border border-border bg-surface-subtle/60 px-3 py-2 text-xs text-muted">Sicil API sözleşmesi yalnız sayfalamayı destekliyor; arama, durum filtresi ve serbest sıralama uygulanmıyor.</p>{page.items.length ? <p className="mb-2 text-xs font-medium text-muted sm:hidden">Tüm belge kolonları için tabloyu yatay kaydırın.</p> : null}<SalesOrderRegister page={page} query={query} /></div>;
}
