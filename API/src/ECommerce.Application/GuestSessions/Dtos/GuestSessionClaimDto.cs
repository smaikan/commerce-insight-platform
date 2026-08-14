using ECommerce.Application.Carts.Dtos;

namespace ECommerce.Application.GuestSessions.Dtos;

// Burada login sonrasında seçilen sepeti ve favori sayısını tek claim cevabında taşıyorum.
public sealed record GuestSessionClaimDto(CartDto Cart, int FavoriteCount);
