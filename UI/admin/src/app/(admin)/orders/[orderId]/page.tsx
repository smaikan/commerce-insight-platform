import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { ApiError } from "@/lib/api/problem";
import { requireAdminPageSession } from "@/lib/auth/session";
import { PageHeader } from "@/modules/admin-shell/components/page-header";
import { getOrder, getOrderReturns } from "@/modules/orders/api";
import { OrderDetail } from "@/modules/orders/components/order-detail";
import { formatOrderDate } from "@/modules/orders/presentation";

// Burada sipariş UUID'sini metadata başlığında yalnız güvenli route bağlamı olarak gösteriyorum.
export async function generateMetadata({ params }: { params: Promise<{ orderId: string }> }): Promise<Metadata> {
  const { orderId } = await params;
  return { title: `Sipariş ${orderId}` };
}

// Burada yönetici sipariş detayını server-side getirip 404 durumunu en yakın route sınırına yönlendiriyorum.
export default async function OrderDetailPage({ params }: { params: Promise<{ orderId: string }> }) {
  const { orderId } = await params;
  if (!isUuid(orderId)) notFound();

  const returnTo = `/orders/${encodeURIComponent(orderId)}`;
  const session = await requireAdminPageSession(returnTo);

  const [orderResult, returnsResult] = await Promise.allSettled([
    getOrder(orderId, session),
    getOrderReturns(orderId, session),
  ]);
  if (orderResult.status === "rejected") {
    const error = orderResult.reason;
    if (error instanceof ApiError && error.problem.status === 404) notFound();
    throw error;
  }
  const order = orderResult.value;
  const returns = returnsResult.status === "fulfilled" ? returnsResult.value : [];

  return (
    <div className="mx-auto w-full max-w-[1480px]">
      <PageHeader
        title={`Sipariş ${order.orderNumber}`}
        description={`${order.id} · ${formatOrderDate(order.createdAt)}`}
        backHref="/orders"
      />
      <OrderDetail order={order} returns={returns} returnsUnavailable={returnsResult.status === "rejected"} />
    </div>
  );
}

// Burada API'ye geçersiz sipariş kimliği göndermeden route parametresinin UUID biçimini doğruluyorum.
function isUuid(value: string): boolean {
  return /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i.test(value);
}
