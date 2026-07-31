namespace ECommerce.Application.Orders.Services;

public interface IOrderReservationPolicy
{
    // Burada checkout sırasında kullanılacak stok rezervasyon süresini uygulama katmanına sağlıyorum.
    TimeSpan ReservationDuration { get; }
}

// Burada yapılandırma henüz sağlanmadığında güvenli on beş dakikalık rezervasyon varsayımını sunuyorum.
public sealed class DefaultOrderReservationPolicy : IOrderReservationPolicy
{
    public static DefaultOrderReservationPolicy Instance { get; } = new();

    // Burada varsayılan politika örneğinin yalnız içeriden oluşturulmasını sağlıyorum.
    private DefaultOrderReservationPolicy()
    {
    }

    public TimeSpan ReservationDuration => TimeSpan.FromMinutes(15);
}
