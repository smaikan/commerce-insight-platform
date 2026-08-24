import type { Metadata } from "next";
import Link from "next/link";
import { redirect } from "next/navigation";
import { PageHeader } from "@/modules/admin-shell/components/page-header";
import { requireAdminPageSession } from "@/lib/auth/session";
import { getPaymentLookups, getPayments } from "@/modules/accounting/payments/api";
import { PaymentRegister } from "@/modules/accounting/payments/components/payment-register";
import { canonicalPaymentPage, parsePaymentListQuery } from "@/modules/accounting/payments/query";

export const metadata: Metadata = { title: "Ödemeler ve Tahsilatlar" };
export default async function PaymentsPage({ searchParams }: { searchParams: Promise<Record<string, string | string[] | undefined>> }) {
  const query = parsePaymentListQuery(await searchParams); const session = await requireAdminPageSession("/accounting/payments");
  const [page, lookups] = await Promise.all([getPayments(query, session), getPaymentLookups(session)]);
  const canonical = canonicalPaymentPage(query.pageNumber, page.totalPages); if (canonical) redirect(canonical === 1 ? "/accounting/payments" : `/accounting/payments?pageNumber=${canonical}`);
  const accountNames = new Map(lookups.accounts.map((item) => [item.id, item.name]));
  return <div className="mx-auto w-full max-w-screen-2xl"><PageHeader title="Ödemeler ve Tahsilatlar" description="Cari açık kalem dağıtımlarını, tedarikçi avanslarını ve finansal etkileri ayrı ödeme sicilinde yönetin." backHref="/accounting" actions={<Link href="/accounting/payments/new" className="inline-flex min-h-10 cursor-pointer items-center rounded-lg bg-primary px-4 text-sm font-semibold text-white hover:bg-primary-hover">Yeni işlem</Link>} />{lookups.truncated ? <p className="mb-4 rounded-lg border border-amber-300 bg-amber-50 px-3 py-2 text-xs text-amber-950">Cari ad eşleştirme listesi ilk 100 kayıtla sınırlı; ödeme kimliği ve detay bağlantısı değişmeden korunur.</p> : null}<PaymentRegister page={page} query={query} accountNames={accountNames} /></div>;
}
