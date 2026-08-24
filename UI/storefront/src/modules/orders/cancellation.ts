import type { components } from "@/generated/api";
import type { ApiProblem } from "@/lib/api/problem";

export type CustomerOrder = components["schemas"]["OrderDto"];
export type OrderCancellationOperation = components["schemas"]["OrderCancellationOperationDto"];
export type OrderCancellationAccessMode = "member" | "guest";

export type OrderCancellationResult =
  | { kind: "completed"; order: CustomerOrder }
  | { kind: "pending"; operation: OrderCancellationOperation };

// Burada API'nin terminal olmayan cancellation durumlarını tek authoritative listede tutuyorum.
export function isCancellationPending(status: OrderCancellationOperation["status"]): boolean {
  return status === 0 || status === 1 || status === 2;
}

// Burada API'nin UTC nextAttemptAt önerisini makul alt ve üst sınırlar içinde polling gecikmesine çeviriyorum.
export function cancellationPollingDelay(nextAttemptAt: string | null, now = Date.now()): number {
  const requestedDelay = nextAttemptAt ? Date.parse(nextAttemptAt) - now : 3_000;
  if (!Number.isFinite(requestedDelay)) return 3_000;
  return Math.min(10_000, Math.max(1_500, requestedDelay));
}

// Burada finansal iptal öncesi açıklamayı siparişin ödeme aşamasına göre yanıltıcı olmayacak biçimde seçiyorum.
export function cancellationConfirmationMessage(orderStatus: number): string {
  if (orderStatus === 2 || orderStatus === 3) {
    return "Ödemeniz alınmışsa iyzico üzerinden iptal veya iade işlemi başlatılır. Tutarın kartınıza yansıma süresi bankanıza göre değişebilir.";
  }

  return "İptal öncesinde ödeme durumu son kez kontrol edilir. Tahsilat yoksa sipariş iptal edilir; stok rezervasyonu ve kupon kullanımı bırakılır.";
}

// Burada her belgelenmiş cancellation ProblemDetails kodunu müşterinin gerçek kurtarma adımına dönüştürüyorum.
export function cancellationProblemMessage(problem: Partial<ApiProblem>): string {
  switch (problem.code) {
    case "order_cancellation_not_allowed":
      return "Sipariş kargoya verildiği veya durumu değiştiği için artık iptal edilemiyor. Güncel sipariş durumu yeniden yüklendi.";
    case "payment_reversal_data_missing":
      return "Bu ödemenin otomatik ve güvenli iadesi için gereken eski işlem bilgileri eksik. Sipariş değiştirilmedi; destek ekibimizle iletişime geçin.";
    case "payment_reversal_rejected":
      return "Ödeme kuruluşu iptal veya iade işlemini kabul etmedi. Sipariş değiştirilmedi; yeniden işlem başlatmadan destek ekibimizle iletişime geçin.";
    case "payment_reversal_manual_review":
      return "İptal işlemi finansal inceleme gerektiriyor. Sipariş değiştirilmedi ve ikinci bir iade başlatılmadı; destek ekibimizle iletişime geçin.";
    case "conflict":
      return "Ödeme veya sipariş durumu bu sırada değişti. Güncel sipariş bilgisi yeniden yüklendi.";
    case "authentication_required":
    case "invalid_access_token":
    case "session_refresh_required":
      return "Oturumunuz sona erdi. Sayfayı yenileyip tekrar giriş yapın.";
    case "invalid_guest_access":
      return "Misafir sipariş erişiminizin süresi dolmuş veya bu sipariş için geçerli değil.";
    default:
      if (problem.status === 404) return "Sipariş veya iptal işlemi bu erişim kapsamında bulunamadı.";
      if (problem.status === 409) return "Sipariş güvenli biçimde iptal edilemedi. Güncel durumu kontrol edip gerekirse destek ekibimizle iletişime geçin.";
      if (problem.status === 503) return "İptal servisine şu anda ulaşılamıyor. Sipariş değiştirilmedi; kısa bir süre sonra durumunu yeniden kontrol edin.";
      return problem.detail || "Sipariş iptal işlemi tamamlanamadı. Siparişiniz değiştirilmedi.";
  }
}

// Burada terminal operasyon durumlarını ikinci bir finansal istek önermeden açık müşteri mesajlarına çeviriyorum.
export function cancellationOperationMessage(operation: OrderCancellationOperation): string {
  if (operation.status === 5) {
    return "İptal işlemi finansal inceleme bekliyor. Siparişiniz henüz değiştirilmedi; ikinci bir iptal isteği göndermeyin ve destek ekibimizle iletişime geçin.";
  }
  if (operation.status === 4) {
    return "İptal veya iade işlemi tamamlanamadı. Siparişiniz değiştirilmedi; yeniden finansal işlem başlatmadan destek ekibimizle iletişime geçin.";
  }
  return operation.reversalType === 1
    ? "İade sonucu ödeme kuruluşuyla doğrulanıyor. Bu işlem sürerken ikinci bir iade isteği gönderilmeyecek."
    : "İptal sonucu ödeme kuruluşuyla doğrulanıyor. Bu işlem sürerken ikinci bir iptal isteği gönderilmeyecek.";
}
