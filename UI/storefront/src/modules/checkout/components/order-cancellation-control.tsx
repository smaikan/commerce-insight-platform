"use client";

import { loadCart } from "@/modules/cart/client/cart-api";
import { forgetActiveCheckoutOrder } from "@/modules/checkout/client/checkout-api";
import type { CustomerOrder, OrderCancellationAccessMode } from "@/modules/orders/cancellation";
import { CustomerOrderCancellationControl } from "@/modules/orders/components/customer-order-cancellation-control";

// Burada checkout'a özgü cart ve aktif ödeme temizliğini ortak cancellation durum makinesinin tamamlanma anına bağlıyorum.
export function OrderCancellationControl({
  orderId,
  orderStatus,
  accessMode,
  label = "Siparişi iptal et ve sepete dön",
  onCancelled,
  onOrderUpdated,
}: {
  orderId: string;
  orderStatus: number;
  accessMode: OrderCancellationAccessMode;
  label?: string;
  onCancelled?: () => void;
  onOrderUpdated?: (order: CustomerOrder) => void;
}) {
  // Burada yalnız doğrulanmış Cancelled OrderDto sonrasında checkout recovery işaretlerini ve cart görünümünü yeniliyorum.
  async function handleOrderUpdated(order: CustomerOrder) {
    forgetActiveCheckoutOrder(order.id);
    await loadCart(true).catch(() => undefined);
    onOrderUpdated?.(order);
    onCancelled?.();
  }

  return (
    <CustomerOrderCancellationControl
      orderId={orderId}
      orderStatus={orderStatus}
      accessMode={accessMode}
      label={label}
      onOrderUpdated={handleOrderUpdated}
    />
  );
}
