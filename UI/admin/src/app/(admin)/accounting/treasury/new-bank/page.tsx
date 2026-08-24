import type { Metadata } from "next";
import { PageHeader } from "@/modules/admin-shell/components/page-header";
import { requireAdminPageSession } from "@/lib/auth/session";
import { FinancialAccountForm } from "@/modules/accounting/treasury/components/financial-account-form";
export const metadata: Metadata = { title: "Yeni Banka Hesabı" };
export default async function NewBankPage() { await requireAdminPageSession("/accounting/treasury/new-bank"); return <div className="mx-auto w-full max-w-3xl"><PageHeader title="Banka hesabı oluştur" description="Banka hareketleri ve atomik transferler için yeni bir TRY hesabı açın." backHref="/accounting/treasury" /><FinancialAccountForm kind="bank" /></div>; }
