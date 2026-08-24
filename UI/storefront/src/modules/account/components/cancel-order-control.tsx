"use client";

import { useRouter } from "next/navigation";

import { CustomerOrderCancellationControl } from "@/modules/orders/components/customer-order-cancellation-control";

// Burada hesap siparişini ortak 200/202 cancellation akışına bağlayıp terminal sonuçta server detail verisini yeniliyorum.
export function CancelOrderControl({ orderId, orderStatus }: { orderId: string; orderStatus: number }) {
  const router = useRouter();

  return (
    <CustomerOrderCancellationControl
      orderId={orderId}
      orderStatus={orderStatus}
      accessMode="member"
      appearance="account"
      onOrderUpdated={() => router.refresh()}
    />
  );
}
