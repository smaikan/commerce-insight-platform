import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { PageHeader } from "@/modules/admin-shell/components/page-header";
import { requireAdminPageSession } from "@/lib/auth/session";
import { ApiError } from "@/lib/api/problem";
import { getPayment, getPaymentLookups } from "@/modules/accounting/payments/api";
import { PaymentDetail } from "@/modules/accounting/payments/components/payment-detail";

export const metadata: Metadata = { title: "Ödeme Detayı" };
export default async function PaymentDetailPage({ params, searchParams }: { params: Promise<{ paymentId: string }>; searchParams: Promise<{ created?: string }> }) {
  const { paymentId } = await params; const { created } = await searchParams; const path = `/accounting/payments/${encodeURIComponent(paymentId)}`; const session = await requireAdminPageSession(path);
  const lookupsPromise = getPaymentLookups(session); let payment;
  try { payment = await getPayment(paymentId, session); } catch (error) { if (error instanceof ApiError && error.problem.status === 404) notFound(); throw error; }
  const lookups = await lookupsPromise;
  return <div className="mx-auto w-full max-w-screen-2xl"><PageHeader title="Ödeme makbuzu" description={`Kayıt ${payment.id.slice(0, 8)} · bakiye ve dağıtımlar API yanıtıdır.`} backHref="/accounting/payments" backLabel="Ödeme siciline dön" />{created ? <p role="status" className="mb-4 rounded-xl border border-emerald-300 bg-emerald-50 px-4 py-3 text-sm font-medium text-emerald-900">Ödeme işlemi başarıyla kaydedildi.</p> : null}<PaymentDetail payment={payment} account={lookups.accounts.find((a) => a.id === payment.currentAccountId)} cashAccount={lookups.cashAccounts.find((a) => a.id === payment.cashAccountId)} bankAccount={lookups.bankAccounts.find((a) => a.id === payment.bankAccountId)} /></div>;
}
