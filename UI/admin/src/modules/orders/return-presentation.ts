import type { ReturnRequestStatus, ReturnType } from "@/modules/orders/types";

// Burada iade ve değişim tiplerini yönetim ekranındaki kısa adlarla eşliyorum.
export function returnTypeLabel(type: ReturnType): string {
  return type === 0 ? "İade" : "Değişim";
}

// Burada iade yaşam döngüsü enumunu kullanıcıya açık durum etiketlerine çeviriyorum.
export function returnStatusLabel(status: ReturnRequestStatus): string {
  return ["Talep geldi", "Onaylandı", "Reddedildi", "Teslim alındı", "Tamamlandı"][status] ?? "Bilinmiyor";
}

// Burada iade durumlarını yalnız gerçek semantik anlamlarına göre renklendiriyorum.
export function returnStatusClass(status: ReturnRequestStatus): string {
  const classes: Record<ReturnRequestStatus, string> = {
    0: "border-orange-200 bg-orange-50 text-orange-800",
    1: "border-blue-200 bg-blue-50 text-blue-800",
    2: "border-red-200 bg-red-50 text-red-800",
    3: "border-amber-200 bg-amber-50 text-amber-800",
    4: "border-emerald-200 bg-emerald-50 text-emerald-800",
  };
  return classes[status];
}
