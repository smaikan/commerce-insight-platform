namespace ECommerce.Application.GuestOrders;

// Burada guest sipariş erişiminin tek merkezden yönetilen süre kurallarını tanımlıyorum.
public static class GuestOrderAccessPolicy
{
    public static readonly TimeSpan SessionLifetime = TimeSpan.FromDays(7);
    public static readonly TimeSpan MagicLinkLifetime = TimeSpan.FromMinutes(30);
}
