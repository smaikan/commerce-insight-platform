import type { Metadata } from "next";
import { PageHeader } from "@/modules/admin-shell/components/page-header";
import { requireAdminPageSession } from "@/lib/auth/session";
import { getPaymentLookups, getPaymentOpenItems } from "@/modules/accounting/payments/api";
import { PaymentCreateForm, PaymentSetupForm } from "@/modules/accounting/payments/components/payment-form";
import { parsePaymentSetup } from "@/modules/accounting/payments/query";

export const metadata: Metadata = { title: "Yeni Ödeme veya Tahsilat" };
export default async function NewPaymentPage({ searchParams }: { searchParams: Promise<Record<string, string | string[] | undefined>> }) {
  const setup = parsePaymentSetup(await searchParams); const session = await requireAdminPageSession("/accounting/payments/new"); const lookups = await getPaymentLookups(session);
  const account = setup ? lookups.accounts.find((item) => item.id === setup.currentAccountId) : undefined;
  const roleValid = Boolean(account && (setup!.type === 1 ? account.type === 1 || account.type === 3 : account.type === 2 || account.type === 3));
  const openPage = setup && roleValid ? await getPaymentOpenItems(setup, session) : null;
  return <div className="mx-auto w-full max-w-screen-xl"><PageHeader title="Yeni ödeme veya tahsilat" description="Önce cari kapsamını seçin; ardından güncel açık kalemleri tek kasa veya banka hareketiyle kapatın." backHref="/accounting/payments" backLabel="Ödeme siciline dön" /><div className="space-y-5"><PaymentSetupForm accounts={lookups.accounts} setup={setup} />{lookups.truncated ? <p className="rounded-lg border border-amber-300 bg-amber-50 px-4 py-3 text-sm text-amber-950">Cari seçim listesi API sınırı nedeniyle ilk 100 aktif kayıtla sınırlı. Aranabilir seçim endpoint’i gereksinimi API önerilerine kaydedildi.</p> : null}{setup && !roleValid ? <div role="alert" className="rounded-xl border border-danger/30 bg-red-50 px-4 py-3 text-sm text-red-900">Seçilen cari bu işlem türü için aktif müşteri/tedarikçi rolüne sahip değil.</div> : null}{setup && account && roleValid && openPage ? <PaymentCreateForm setup={setup} account={account} openItems={openPage.items} cashAccounts={lookups.cashAccounts} bankAccounts={lookups.bankAccounts} /> : null}</div></div>;
}
