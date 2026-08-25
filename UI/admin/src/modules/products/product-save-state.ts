import type { ProductActionState } from "@/modules/products/types";

type ProductSaveNavigator = {
  replace: (href: string) => void;
};

// Burada framework navigasyonu sürse bile Server Action yeni sonuç döndürdüğünde kayıt bekleme durumunu kapatıyorum.
export function isProductActionAwaitingResult(
  actionPending: boolean,
  currentState: ProductActionState,
  stateAtSubmit: ProductActionState,
): boolean {
  if (currentState.status === "success" && currentState.completionToken) return false;
  return actionPending && currentState === stateAtSubmit;
}

// Burada başarılı kayıttan sonra tek bir route geçişi başlatıp eşzamanlı refresh yarışını önlüyorum.
export function navigateAfterSuccessfulProductSave(
  navigator: ProductSaveNavigator,
  productId: string,
  mode: "create" | "edit",
): void {
  navigator.replace(`/products/${encodeURIComponent(productId)}?${mode === "create" ? "created" : "saved"}=1`);
}
