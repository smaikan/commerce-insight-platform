using ECommerce.Application.Carts.Dtos;
using MediatR;

namespace ECommerce.Application.Carts.Commands.MergeGuestCart;

// Burada giriş sonrası misafir sepetini kayıtlı kullanıcının sepetiyle birleştirme isteğini taşıyorum.
public sealed record MergeGuestCartCommand(string SessionId) : IRequest<CartDto>;
