import type { Metadata } from "next";
import Link from "next/link";
import { notFound } from "next/navigation";
import { PageHeader } from "@/modules/admin-shell/components/page-header";
import { requireAdminPageSession } from "@/lib/auth/session";
import { getCashAccounts, getCashStatement } from "@/modules/accounting/treasury/api";
import { FinancialStatement } from "@/modules/accounting/treasury/components/financial-statement";
import { formatMoney } from "@/modules/accounting/treasury/presentation";

export const metadata: Metadata = { title: "Kasa Ekstresi" };
export default async function CashStatementPage({ params, searchParams }: { params: Promise<{ accountId: string }>; searchParams: Promise<Record<string, string | undefined>> }) {
  const { accountId } = await params; const notices = await searchParams; const path = `/accounting/treasury/cash/${encodeURIComponent(accountId)}`; const session = await requireAdminPageSession(path);
  const accounts = await getCashAccounts(session); const account = accounts.find((item) => item.id === accountId); if (!account) notFound(); const statement = await getCashStatement(accountId, session);
  return <div className="mx-auto w-full max-w-screen-2xl"><PageHeader title={account.name} description={`Kasa ${account.code} · ${account.isActive ? "Aktif" : "Pasif"}`} backHref="/accounting/treasury" backLabel="Kasa ve bankaya dön" actions={<Link href="/accounting/treasury?view=manual" className="inline-flex min-h-10 cursor-pointer items-center rounded-lg bg-primary px-4 text-sm font-semibold text-white">Manuel hareket</Link>} />{notices.created || notices.createdTransaction ? <p role="status" className="mb-4 rounded-xl border border-emerald-300 bg-emerald-50 px-4 py-3 text-sm font-medium text-emerald-900">{notices.created ? "Kasa hesabı oluşturuldu." : "Finans hareketi kaydedildi; ekstre API'den yeniden okundu."}</p> : null}<section className="mb-5 flex flex-wrap items-end justify-between gap-4 rounded-xl border border-primary/20 bg-primary-soft/20 p-5"><div><p className="text-xs font-semibold uppercase tracking-wider text-muted">API tarafından türetilen bakiye</p><p className="mt-1 text-3xl font-semibold tabular-nums">{formatMoney(account.balance, account.currencyCode)}</p></div><p className="max-w-xl text-sm leading-6 text-muted">Bakiye düzenlenemez. Geriye tarihli hareketler eklendiğinde tüm “hareket sonrası bakiye” değerleri API tarafından yeniden hesaplanır.</p></section><FinancialStatement transactions={statement} accountPath={path} /></div>;
}
