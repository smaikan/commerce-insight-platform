import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { ApiError } from "@/lib/api/problem";
import { requireAdminPageSession } from "@/lib/auth/session";
import { PageHeader } from "@/modules/admin-shell/components/page-header";
import { getCustomer, getCustomerOrders } from "@/modules/customers/api";
import { CustomerDetailPanel } from "@/modules/customers/components/customer-detail-panel";

// Burada müşteri kimliğini metadata içinde yalnızca güvenli route bağlamı olarak kullanıyorum.
export async function generateMetadata({ params }: { params: Promise<{ userId: string }> }): Promise<Metadata> {
  const { userId } = await params;
  return { title: `Müşteri ${userId}` };
}

// Burada müşteri detayını yönetici oturumuyla getirip 404 sonucunu route sınırına iletiyorum.
export default async function CustomerDetailPage({ params, searchParams }: { params: Promise<{ userId: string }>; searchParams: Promise<Record<string, string | string[] | undefined>> }) {
  const { userId } = await params;
  const notices = await searchParams;
  const session = await requireAdminPageSession(`/customers/${encodeURIComponent(userId)}`);
  let customer;
  let orders;
  try {
    [customer, orders] = await Promise.all([getCustomer(userId, session), getCustomerOrders(userId, session)]);
  } catch (error) {
    if (error instanceof ApiError && error.problem.status === 404) notFound();
    throw error;
  }

  return (
    <div className="mx-auto w-full max-w-5xl">
      <PageHeader title={`${customer.firstName} ${customer.lastName}`.trim()} description={customer.email} backHref="/customers" />
      {notices.updated === "role" || notices.updated === "status" ? <p className="mb-5 rounded-xl border border-emerald-300 bg-emerald-50 px-4 py-3 text-sm font-medium text-emerald-900" role="status">Hesap bilgisi güncellendi.</p> : null}
      {notices.error === "conflict" ? <p className="mb-5 rounded-xl border border-amber-300 bg-amber-50 px-4 py-3 text-sm font-medium text-amber-900" role="alert">Son aktif yönetici hesabının rolü veya durumu değiştirilemez.</p> : null}
      {notices.error === "forbidden" ? <p className="mb-5 rounded-xl border border-red-300 bg-red-50 px-4 py-3 text-sm font-medium text-red-900" role="alert">Bu işlem için etkin yönetici yetkisi gerekiyor.</p> : null}
      {notices.error === "failed" ? <p className="mb-5 rounded-xl border border-red-300 bg-red-50 px-4 py-3 text-sm font-medium text-red-900" role="alert">Hesap bilgisi güncellenemedi. Lütfen tekrar deneyin.</p> : null}
      <CustomerDetailPanel customer={customer} orders={orders} />
    </div>
  );
}
