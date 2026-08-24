import type { AdminMutationResult } from "@/lib/admin/mutation-result";
import type { ReturnRequest } from "@/modules/orders/types";

export type ReturnMutationResult = AdminMutationResult & {
  returnRequest?: ReturnRequest;
};

type ReturnAction = (
  previousState: AdminMutationResult | null,
  formData: FormData,
) => Promise<ReturnMutationResult>;

// Burada iade mutasyonunun bekleme durumunu route yenilemesinden ayırıp bütün sonuç yollarında kesin kapatıyorum.
export async function runReturnAction(
  action: ReturnAction,
  formData: FormData,
  setPending: (pending: boolean) => void,
): Promise<ReturnMutationResult> {
  setPending(true);
  try {
    return await action(null, formData);
  } catch {
    return {
      status: "error",
      message: "İade talebi güncellenemedi. Bağlantıyı kontrol edip tekrar deneyin.",
    };
  } finally {
    setPending(false);
  }
}
