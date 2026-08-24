import type { StockMovementDirection, StockMovementType } from "./types";

export const stockMovementDirectionOptions = [
  { value: 1 as StockMovementDirection, label: "Giriş" },
  { value: 2 as StockMovementDirection, label: "Çıkış" },
] as const;

export const stockMovementTypeOptions = [
  { value: 1 as StockMovementType, label: "Açılış bakiyesi", systemOwned: true },
  { value: 10 as StockMovementType, label: "Satın alma", allowedDirections: [1] },
  { value: 11 as StockMovementType, label: "Satın alma iadesi", allowedDirections: [2] },
  { value: 20 as StockMovementType, label: "Satış", systemOwned: true },
  { value: 21 as StockMovementType, label: "Satış iadesi", systemOwned: true },
  { value: 22 as StockMovementType, label: "Muhasebe satışı", systemOwned: true },
  { value: 23 as StockMovementType, label: "Muhasebe satış iadesi", systemOwned: true },
  { value: 30 as StockMovementType, label: "Manuel düzeltme", allowedDirections: [1, 2] },
  { value: 31 as StockMovementType, label: "Stok sayım düzeltmesi", allowedDirections: [1, 2] },
  { value: 40 as StockMovementType, label: "Kayıp", allowedDirections: [2] },
  { value: 41 as StockMovementType, label: "Hasar", allowedDirections: [2] },
  { value: 42 as StockMovementType, label: "Son kullanma", allowedDirections: [2] },
  { value: 50 as StockMovementType, label: "Transfer girişi", allowedDirections: [1] },
  { value: 51 as StockMovementType, label: "Transfer çıkışı", allowedDirections: [2] },
  { value: 60 as StockMovementType, label: "İptal", systemOwned: true },
] as const;

export const manualStockMovementTypeOptions = stockMovementTypeOptions.filter(
  (option): option is (typeof stockMovementTypeOptions)[number] & { allowedDirections: readonly StockMovementDirection[] } =>
    "allowedDirections" in option,
);

// Burada hareket türünün yönetim ekranında açıklanabilir adını döndürüyorum.
export function stockMovementTypeLabel(value: StockMovementType): string {
  return stockMovementTypeOptions.find((option) => option.value === value)?.label || "Bilinmeyen hareket";
}

// Burada hareket yönünü yalnız stok giriş veya çıkış metnine çeviriyorum.
export function stockMovementDirectionLabel(value: StockMovementDirection): string {
  return stockMovementDirectionOptions.find((option) => option.value === value)?.label || "Bilinmeyen yön";
}

// Burada istemci ve Server Action doğrulamasında aynı izinli manuel tür listesini kullanıyorum.
export function supportsManualStockMovement(type: number, direction: number): type is StockMovementType {
  const option = manualStockMovementTypeOptions.find((item) => item.value === type);
  return Boolean(option?.allowedDirections.includes(direction as StockMovementDirection));
}
