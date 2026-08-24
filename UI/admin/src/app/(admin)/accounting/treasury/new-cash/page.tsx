import type { Metadata } from "next";
import { PageHeader } from "@/modules/admin-shell/components/page-header";
import { requireAdminPageSession } from "@/lib/auth/session";
import { FinancialAccountForm } from "@/modules/accounting/treasury/components/financial-account-form";
export const metadata: Metadata = { title: "Yeni Kasa" };
export default async function NewCashPage() { await requireAdminPageSession("/accounting/treasury/new-cash"); return <div className="mx-auto w-full max-w-3xl"><PageHeader title="Kasa hesabı oluştur" description="Nakit hareketleri için bakiyesi işlemlerden türetilen yeni bir TRY kasası açın." backHref="/accounting/treasury" /><FinancialAccountForm kind="cash" /></div>; }
