import type { Metadata } from "next";
import { notFound, redirect } from "next/navigation";
import { ApiError } from "@/lib/api/problem";
import { requireAdminPageSession } from "@/lib/auth/session";
import { PageHeader } from "@/modules/admin-shell/components/page-header";
import { getSalesLookups, getSalesOrder } from "@/modules/accounting/sales/api";
import { SalesDocumentForm } from "@/modules/accounting/sales/components/sales-document-form";
import { salesOrderToDraft } from "@/modules/accounting/sales/presentation";

export const metadata: Metadata = { title: "Muhasebe Satışını Düzenle" };
export default async function EditSalesOrderPage({ params }: { params: Promise<{ salesOrderId: string }> }) { const { salesOrderId } = await params; const href = `/accounting/sales-orders/${encodeURIComponent(salesOrderId)}/edit`; const session = await requireAdminPageSession(href); let order; try { order = await getSalesOrder(salesOrderId, session); } catch (error) { if (error instanceof ApiError && error.problem.status === 404) notFound(); throw error; } if (order.status !== 1) redirect(`/accounting/sales-orders/${encodeURIComponent(order.id)}`); const lookups = await getSalesLookups(session); return <div className="mx-auto w-full max-w-screen-2xl"><PageHeader title={`${order.orderNumber} taslağını düzenle`} description="GET → PUT round-trip ile ticari başlık ve tam satır listesi korunur." backHref={`/accounting/sales-orders/${encodeURIComponent(order.id)}`} backLabel="Satış detayına dön" /><SalesDocumentForm mode="sales-order" orderId={order.id} initialDraft={salesOrderToDraft(order)} customers={lookups.customers} variants={lookups.variants} lookupTruncated={lookups.truncated} /></div>; }
