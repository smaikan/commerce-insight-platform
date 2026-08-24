import type { Metadata } from "next";
import { requireAdminPageSession } from "@/lib/auth/session";
import { PageHeader } from "@/modules/admin-shell/components/page-header";
import { getSalesLookups } from "@/modules/accounting/sales/api";
import { SalesDocumentForm } from "@/modules/accounting/sales/components/sales-document-form";
import { newSalesOrderDraft } from "@/modules/accounting/sales/presentation";

export const metadata: Metadata = { title: "Doğrudan Satış Faturası Oluştur" };
export default async function NewSalesInvoicePage() { const session = await requireAdminPageSession("/accounting/sales-invoices/new"); const lookups = await getSalesLookups(session); return <div className="mx-auto w-full max-w-screen-2xl"><PageHeader title="Doğrudan satış faturası oluştur" description="Aynı transaction içinde bağlı bir muhasebe satışı ve iç satış faturası taslağı oluşturun." backHref="/accounting/sales-invoices" /><SalesDocumentForm mode="direct-invoice" initialDraft={newSalesOrderDraft(crypto.randomUUID(), true)} customers={lookups.customers} variants={lookups.variants} lookupTruncated={lookups.truncated} /></div>; }
