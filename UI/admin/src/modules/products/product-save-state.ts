import type { ProductActionState } from "@/modules/products/types";

// Burada framework navigasyonu sürse bile Server Action yeni sonuç döndürdüğünde kayıt bekleme durumunu kapatıyorum.
export function isProductActionAwaitingResult(
  actionPending: boolean,
  currentState: ProductActionState,
  stateAtSubmit: ProductActionState,
): boolean {
  return actionPending && currentState === stateAtSubmit;
}
