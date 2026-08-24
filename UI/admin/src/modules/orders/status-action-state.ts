import type { AdminMutationResult } from "@/lib/admin/mutation-result";
import type { OrderStatus } from "@/modules/orders/types";

export type OrderStatusMutationResult = AdminMutationResult & {
  orderStatus?: OrderStatus;
};

type OrderStatusAction = (
  previousState: AdminMutationResult | null,
  formData: FormData,
) => Promise<OrderStatusMutationResult>;

// Burada mutation pending'ini React route transition'ından ayırıp başarı, hata ve transport rejection yollarında kesin kapatıyorum.
export async function runOrderStatusAction(
  action: OrderStatusAction,
  formData: FormData,
  setPending: (pending: boolean) => void,
): Promise<OrderStatusMutationResult> {
  setPending(true);
  try {
    return await action(null, formData);
  } catch {
    return {
      status: "error",
      message: "Sipariş durumu güncellenemedi. Bağlantıyı kontrol edip tekrar deneyin.",
    };
  } finally {
    setPending(false);
  }
}
