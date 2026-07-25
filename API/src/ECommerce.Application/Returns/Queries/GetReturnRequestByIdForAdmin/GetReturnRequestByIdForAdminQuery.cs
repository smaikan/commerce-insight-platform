using ECommerce.Application.Returns.Dtos;
using MediatR;

namespace ECommerce.Application.Returns.Queries.GetReturnRequestByIdForAdmin;

// Burada yöneticinin tek iade talebi ayrıntısını getirme isteğini taşıyorum.
public sealed record GetReturnRequestByIdForAdminQuery(Guid ReturnRequestId) : IRequest<ReturnRequestDto>;
