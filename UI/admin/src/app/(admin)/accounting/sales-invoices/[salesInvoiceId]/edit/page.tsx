import type { Metadata } from "next";
import { notFound, redirect } from "next/navigation";
import { ApiError } from "@/lib/api/problem";
import { requireAdminPageSession } from "@/lib/auth/session";
import { PageHeader } from "@/modules/admin-shell/components/page-header";
import { getSalesInvoice, getSalesLookups } from "@/modules/accounting/sales/api";
import { SalesInvoiceEditForm } from "@/modules/accounting/sales/components/sales-invoice-edit-form";
import { salesInvoiceToDraft } from "@/modules/accounting/sales/presentation";

export const metadata: Metadata = { title: "Satış Faturasını Düzenle" };
export default async function EditSalesInvoicePage({ params }: { params: Promise<{ salesInvoiceId: string }> }) { const { salesInvoiceId } = await params; const href = `/accounting/sales-invoices/${encodeURIComponent(salesInvoiceId)}/edit`; const session = await requireAdminPageSession(href); let invoice; try { invoice = await getSalesInvoice(salesInvoiceId, session); } catch (error) { if (error instanceof ApiError && error.problem.status === 404) notFound(); throw error; } if (invoice.status !== 1) redirect(`/accounting/sales-invoices/${encodeURIComponent(invoice.id)}`); const lookups = await getSalesLookups(session); return <div className="mx-auto w-full max-w-screen-2xl"><PageHeader title={`${invoice.invoiceNumber} taslağını düzenle`} description="Fatura başlığı ve bağlı muhasebe satışının tam satır listesi birlikte güncellenir." backHref={`/accounting/sales-invoices/${encodeURIComponent(invoice.id)}`} backLabel="Fatura detayına dön" /><SalesInvoiceEditForm invoice={invoice} initialDraft={salesInvoiceToDraft(invoice)} variants={lookups.variants} lookupTruncated={lookups.truncated} /></div>; }
