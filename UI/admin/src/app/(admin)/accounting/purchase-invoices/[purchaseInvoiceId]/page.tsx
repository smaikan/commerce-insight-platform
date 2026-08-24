import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { ApiError } from "@/lib/api/problem";
import { requireAdminPageSession } from "@/lib/auth/session";
import { PageHeader } from "@/modules/admin-shell/components/page-header";
import { getAvailableStockMovements, getExpenseCategories, getPurchaseInvoice, getPurchaseInvoiceCostHistory, getPurchaseInvoiceExpenses } from "@/modules/accounting/purchases/api";
import { PurchaseInvoiceDetail } from "@/modules/accounting/purchases/components/purchase-invoice-detail";
import { invoiceStatusClass, invoiceStatusLabel } from "@/modules/accounting/purchases/presentation";
import type { AvailableStockMovement, ExpenseCategory, ProductVariantCostHistory, PurchaseInvoiceExpense } from "@/modules/accounting/purchases/types";

export const metadata: Metadata = { title: "Alış Faturası Detayı" };

export default async function PurchaseInvoiceDetailPage({ params, searchParams }: { params: Promise<{ purchaseInvoiceId: string }>; searchParams: Promise<Record<string, string | string[] | undefined>> }) {
  const [{ purchaseInvoiceId }, query] = await Promise.all([params, searchParams]);
  const returnTo = `/accounting/purchase-invoices/${encodeURIComponent(purchaseInvoiceId)}`;
  const session = await requireAdminPageSession(returnTo);
  let invoice;
  try { invoice = await getPurchaseInvoice(purchaseInvoiceId, session); }
  catch (error) { if (error instanceof ApiError && error.problem.status === 404) notFound(); throw error; }

  // Burada belge ana verisini ikincil gider, seçim ve maliyet okumalarından bağımsız tutarak kısmi görünümü koruyorum.
  const [expensesResult, categoriesResult, availableResult, historyResult] = await Promise.all([
    getPurchaseInvoiceExpenses(invoice.id, session).then((value) => ({ ok: true as const, value })).catch(() => ({ ok: false as const, value: [] as PurchaseInvoiceExpense[] })),
    getExpenseCategories(1, 100, session).then((value) => ({ ok: true as const, value: value.items })).catch(() => ({ ok: false as const, value: [] as ExpenseCategory[] })),
    invoice.status === 1 ? Promise.all(invoice.lines.map(async (line) => [line.id, await getAvailableStockMovements(line.productVariantId, session)] as const)).then((entries) => ({ ok: true as const, value: Object.fromEntries(entries) as Record<string, AvailableStockMovement[]> })).catch(() => ({ ok: false as const, value: {} as Record<string, AvailableStockMovement[]> })) : Promise.resolve({ ok: true as const, value: {} as Record<string, AvailableStockMovement[]> }),
    invoice.status !== 1 ? getPurchaseInvoiceCostHistory(invoice, session).then((value) => ({ ok: true as const, value })).catch(() => ({ ok: false as const, value: [] as ProductVariantCostHistory[] })) : Promise.resolve({ ok: true as const, value: [] as ProductVariantCostHistory[] }),
  ]);
  const secondaryUnavailable = !expensesResult.ok || !categoriesResult.ok || !availableResult.ok || !historyResult.ok;
  const created = query.created === "1";
  const updated = query.updated === "1";
  return <div className="mx-auto w-full max-w-screen-2xl"><PageHeader title={invoice.invoiceNumber} description={invoice.currentAccountName} backHref="/accounting/purchase-invoices" backLabel="Alış faturalarına dön" actions={<span className={`inline-flex rounded-md border px-2.5 py-1 text-xs font-bold ${invoiceStatusClass(invoice.status)}`}>{invoiceStatusLabel(invoice.status)}</span>} />{created || updated ? <p role="status" className="mb-4 rounded-xl border border-emerald-300 bg-emerald-50 px-4 py-3 text-sm font-medium text-emerald-900">{created ? "Alış faturası taslağı oluşturuldu. Şimdi satır tahsislerini tamamlayabilirsiniz." : "Alış faturası taslağı güncellendi."}</p> : null}<PurchaseInvoiceDetail invoice={invoice} expenses={expensesResult.value} categories={categoriesResult.value} availableByLine={availableResult.value} costHistory={historyResult.value} secondaryDataUnavailable={secondaryUnavailable} /></div>;
}
