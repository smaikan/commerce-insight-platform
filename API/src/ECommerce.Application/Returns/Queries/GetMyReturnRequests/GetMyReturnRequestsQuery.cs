using ECommerce.Application.Common.Models;
using ECommerce.Application.Returns.Dtos;
using ECommerce.Domain.Enums;
using MediatR;

namespace ECommerce.Application.Returns.Queries.GetMyReturnRequests;

// Burada kullanıcının kendi iade taleplerini sayfalı getirme isteğini taşıyorum.
public sealed record GetMyReturnRequestsQuery(
    int PageNumber = 1,
    int PageSize = 20,
    ReturnType? Type = null,
    ReturnRequestStatus? Status = null,
    DateTime? CreatedFromUtc = null,
    DateTime? CreatedToUtc = null) : IRequest<PagedResult<ReturnRequestSummaryDto>>;
