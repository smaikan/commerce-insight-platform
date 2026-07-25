using ECommerce.Application.Common.Models;
using ECommerce.Application.Returns.Dtos;
using ECommerce.Domain.Enums;
using MediatR;

namespace ECommerce.Application.Returns.Queries.GetReturnRequests;

// Burada yöneticinin iade taleplerini filtreleyerek sayfalı getirme isteğini taşıyorum.
public sealed record GetReturnRequestsQuery(
    int PageNumber = 1,
    int PageSize = 20,
    Guid? OrderId = null,
    ReturnType? Type = null,
    ReturnRequestStatus? Status = null,
    DateTime? CreatedFromUtc = null,
    DateTime? CreatedToUtc = null) : IRequest<PagedResult<ReturnRequestSummaryDto>>;
