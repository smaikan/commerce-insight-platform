using ECommerce.Application.Returns.Dtos;
using MediatR;

namespace ECommerce.Application.Returns.Queries.GetReturnRequestById;

// Burada kullanıcının kendi tek iade talebi ayrıntısını getirme isteğini taşıyorum.
public sealed record GetReturnRequestByIdQuery(Guid ReturnRequestId) : IRequest<ReturnRequestDto>;
