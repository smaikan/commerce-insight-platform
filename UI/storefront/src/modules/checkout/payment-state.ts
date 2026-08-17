import type { CheckoutOrder } from "@/modules/checkout/types";

export type AuthoritativePaymentState = "paid" | "pending" | "failed";

// Burada callback query'sini kullanmadan yalnız sahiplik denetimli sipariş ve en güncel iyzico ödeme kaydından ekran durumunu çıkarıyorum.
export function authoritativePaymentState(order: CheckoutOrder): AuthoritativePaymentState {
  if (order.grandTotal === 0 || order.status === 2) return "paid";

  const latestIyzicoPayment = order.payments
    .filter((payment) => payment.provider === 1)
    .sort((left, right) => Date.parse(right.createdAt) - Date.parse(left.createdAt))[0];

  if (latestIyzicoPayment?.status === 1) return "paid";
  if (latestIyzicoPayment?.status === 2 || latestIyzicoPayment?.status === 4) return "failed";
  return "pending";
}
