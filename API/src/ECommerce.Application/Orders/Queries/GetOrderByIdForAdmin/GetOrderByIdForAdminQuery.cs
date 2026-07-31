using ECommerce.Application.Orders.Dtos;
using MediatR;

namespace ECommerce.Application.Orders.Queries.GetOrderByIdForAdmin;

// Burada yetkili yönetim ekranı için belirli sipariş detayını getirme isteğini taşıyorum.
public sealed record GetOrderByIdForAdminQuery(Guid OrderId) : IRequest<OrderDto>;
