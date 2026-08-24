import type { Metadata } from "next";
import { ApiError } from "@/lib/api/problem";
import { requireAdminPageSession } from "@/lib/auth/session";
import { PageHeader } from "@/modules/admin-shell/components/page-header";
import { AccountingLoadProblem } from "@/modules/accounting/core/components/accounting-load-problem";
import { getPurchaseInvoiceLookups } from "@/modules/accounting/purchases/api";
import { PurchaseInvoiceForm } from "@/modules/accounting/purchases/components/purchase-invoice-form";
import { newPurchaseInvoiceDraft } from "@/modules/accounting/purchases/presentation";

export const metadata: Metadata = { title: "Alış Faturası Oluştur" };

export default async function NewPurchaseInvoicePage() {
  const session = await requireAdminPageSession("/accounting/purchase-invoices/new");
  let lookups;
  try { lookups = await getPurchaseInvoiceLookups(session); }
  catch (error) { if (error instanceof ApiError) return <AccountingLoadProblem problem={error.problem} retryHref="/accounting/purchase-invoices/new" />; throw error; }
  return <div className="mx-auto w-full max-w-screen-2xl"><PageHeader title="Alış faturası oluştur" description="Belge başlığını ve ilk satırları girin. Kayıttan sonra mevcut Purchase stok hareketlerini tahsis edebilirsiniz." backHref="/accounting/purchase-invoices" backLabel="Alış faturalarına dön" /><PurchaseInvoiceForm initialDraft={newPurchaseInvoiceDraft()} suppliers={lookups.suppliers} variants={lookups.variants} lookupTruncated={lookups.truncated} /></div>;
}
