import type { OrderStatus } from "@/modules/orders/types";

export type ManagedOrderStatus = 0 | 1 | 2 | 3 | 4 | 5 | 6;

export type OrderStatusTransition = {
  value: OrderStatus;
  label: string;
  description: string;
};

// Burada genel durum endpoint'inin izin verdiği geçişleri iade ve refund durumlarını dışarıda bırakarak tanımlıyorum.
const transitions: Partial<Record<OrderStatus, OrderStatusTransition[]>> = {
  0: [
    { value: 1, label: "Onaylandı", description: "Siparişi ödeme aşamasına hazır olarak işaretler." },
    { value: 6, label: "İptal edildi", description: "Siparişi iptal eder; stok ve kupon etkileri backend tarafından geri alınır." },
  ],
  1: [
    { value: 2, label: "Ödeme alındı", description: "Yalnız başarılı ödeme kaydı bulunan siparişte uygulanabilir." },
    { value: 6, label: "İptal edildi", description: "Siparişi iptal eder; stok ve kupon etkileri backend tarafından geri alınır." },
  ],
  2: [
    { value: 3, label: "Hazırlanıyor", description: "Siparişi paketleme ve hazırlık sürecine taşır." },
    { value: 6, label: "İptal edildi", description: "Kargoya verilmemiş siparişi iptal eder; ödeme iadesi oluşturmaz." },
  ],
  3: [
    { value: 4, label: "Kargoya verildi", description: "Taşıyıcı ve takip numarasıyla sevkiyatı başlatır." },
    { value: 6, label: "İptal edildi", description: "Kargoya verilmemiş siparişi iptal eder; ödeme iadesi oluşturmaz." },
  ],
  4: [
    { value: 5, label: "Teslim edildi", description: "Siparişin müşteriye teslim edildiğini kaydeder." },
    { value: 4, label: "Kargo bilgisini güncelle", description: "Mevcut taşıyıcı ve takip bilgilerini günceller." },
  ],
};

// Burada mevcut sipariş durumundan gösterilebilecek belgelenmiş yönetim hedeflerini döndürüyorum.
export function orderStatusTransitions(status: OrderStatus): OrderStatusTransition[] {
  return transitions[status] ?? [];
}

// Burada istemciden gelen hedefi genel durum endpoint'inin güvenli allowlist'iyle doğruluyorum.
export function isManagedOrderStatus(value: number): value is ManagedOrderStatus {
  return value === 0 || value === 1 || value === 2 || value === 3 || value === 4 || value === 5 || value === 6;
}
