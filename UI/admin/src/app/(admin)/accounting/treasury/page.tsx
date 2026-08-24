import type { Metadata } from "next";
import { PageHeader } from "@/modules/admin-shell/components/page-header";
import { requireAdminPageSession } from "@/lib/auth/session";
import { getBankAccounts, getCashAccounts } from "@/modules/accounting/treasury/api";
import { TreasuryWorkspace } from "@/modules/accounting/treasury/components/treasury-workspace";
import { parseTreasuryView } from "@/modules/accounting/treasury/query";

export const metadata: Metadata = { title: "Kasa ve Banka" };
export default async function TreasuryPage({ searchParams }: { searchParams: Promise<Record<string, string | string[] | undefined>> }) { const view = parseTreasuryView(await searchParams); const session = await requireAdminPageSession("/accounting/treasury"); const [cashAccounts, bankAccounts] = await Promise.all([getCashAccounts(session), getBankAccounts(session)]); return <div className="mx-auto w-full max-w-screen-2xl"><PageHeader title="Kasa ve Banka" description="Türetilmiş bakiyeleri, finans ekstrelerini ve kontrollü hazine komutlarını tek operasyon alanında yönetin." backHref="/accounting" /><TreasuryWorkspace view={view} cashAccounts={cashAccounts} bankAccounts={bankAccounts} /></div>; }
