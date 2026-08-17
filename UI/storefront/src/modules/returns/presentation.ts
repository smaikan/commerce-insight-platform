import { formatAccountDate, formatAccountDateTime } from "@/modules/account/presentation";

export const RETURN_STATUS_LABELS: Record<number, string> = {
  0: "İnceleniyor",
  1: "Onaylandı",
  2: "Reddedildi",
  3: "Ürün teslim alındı",
  4: "Tamamlandı",
};

// Burada API enum değerlerini talebin gerçek durumunu koruyan müşteri etiketlerine çeviriyorum.
export function returnStatusLabel(status: number): string {
  return RETURN_STATUS_LABELS[status] ?? "Durum güncelleniyor";
}

export function returnTypeLabel(type: number): string {
  return type === 1 ? "Değişim" : "İade";
}

export { formatAccountDate, formatAccountDateTime };
