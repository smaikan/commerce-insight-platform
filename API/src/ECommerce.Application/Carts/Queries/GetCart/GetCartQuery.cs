using ECommerce.Application.Carts.Dtos;
using MediatR;

namespace ECommerce.Application.Carts.Queries.GetCart;

// Burada mevcut kullanıcı veya misafir oturumunun sepetini getirecek sorguyu taşıyorum.
public sealed record GetCartQuery(string? SessionId = null) : IRequest<CartDto>;
