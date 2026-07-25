using ECommerce.Domain.Entities;

namespace ECommerce.Application.Common.Interfaces;

// Burada sipariş olaylarının güvenilir e-posta outbox bildirimlerine dönüştürülmesi sözleşmesini tanımlıyorum.
public interface IOrderNotificationService
{
    // Burada yeni oluşturulmuş sipariş için müşteri bildirimini aynı iş akışına eklemeyi tanımlıyorum.
    Task QueueOrderCreatedAsync(Order order, CancellationToken cancellationToken = default);

    // Burada tamamlanmış ödeme sonucunun müşteriye bildirilecek outbox kaydına dönüştürülmesini tanımlıyorum.
    Task QueuePaymentResultAsync(
        Order order,
        Payment payment,
        CancellationToken cancellationToken = default);

    // Burada sipariş yaşam döngüsü değişikliğinin müşteriye bildirilecek outbox kaydına dönüştürülmesini tanımlıyorum.
    Task QueueOrderStatusChangedAsync(Order order, CancellationToken cancellationToken = default);

    // Burada müşterinin açtığı iade veya değişim talebini sipariş snapshot'ıyla birlikte outbox'a eklemeyi tanımlıyorum.
    Task QueueReturnRequestedAsync(
        ReturnRequest returnRequest,
        Order order,
        CancellationToken cancellationToken = default);

    // Burada iade veya değişim talebinin iş akışı değişikliğini müşteriye bildirilecek outbox kaydına dönüştürmeyi tanımlıyorum.
    Task QueueReturnStatusChangedAsync(
        ReturnRequest returnRequest,
        Order order,
        CancellationToken cancellationToken = default);
}
