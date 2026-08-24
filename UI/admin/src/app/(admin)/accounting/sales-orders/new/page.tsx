import type { Metadata } from "next";
import { requireAdminPageSession } from "@/lib/auth/session";
import { PageHeader } from "@/modules/admin-shell/components/page-header";
import { getSalesLookups } from "@/modules/accounting/sales/api";
import { SalesDocumentForm } from "@/modules/accounting/sales/components/sales-document-form";
import { newSalesOrderDraft } from "@/modules/accounting/sales/presentation";

export const metadata: Metadata = { title: "Muhasebe Satışı Oluştur" };
export default async function NewSalesOrderPage() { const session = await requireAdminPageSession("/accounting/sales-orders/new"); const lookups = await getSalesLookups(session); return <div className="mx-auto w-full max-w-screen-2xl"><PageHeader title="Muhasebe satışı oluştur" description="Müşteri carisine bağlı, taslak ve stok etkisiz bir ön muhasebe satış belgesi hazırlayın." backHref="/accounting/sales-orders" /><SalesDocumentForm mode="sales-order" initialDraft={newSalesOrderDraft(crypto.randomUUID())} customers={lookups.customers} variants={lookups.variants} lookupTruncated={lookups.truncated} /></div>; }
