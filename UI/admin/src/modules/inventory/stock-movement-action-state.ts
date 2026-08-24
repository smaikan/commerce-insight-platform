import type { StockMovementActionState } from "@/modules/inventory/types";

type StockMovementAction = (
  previousState: StockMovementActionState,
  formData: FormData,
) => Promise<StockMovementActionState>;

// Burada stok hareketi pending durumunu route yenilemesinden ayırıp bütün sonuç yollarında kesin kapatıyorum.
export async function runStockMovementAction(
  action: StockMovementAction,
  formData: FormData,
  setPending: (pending: boolean) => void,
): Promise<StockMovementActionState> {
  setPending(true);
  try {
    return await action({ status: "idle" }, formData);
  } catch {
    return {
      status: "error",
      message: "Stok hareketleri kaydedilemedi. Bağlantıyı kontrol edip tekrar deneyin.",
    };
  } finally {
    setPending(false);
  }
}
