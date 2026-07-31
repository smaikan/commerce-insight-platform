using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Application.Returns.Dtos;
using MediatR;

namespace ECommerce.Application.Returns.Queries.GetMyReturnRequests;

public sealed class GetMyReturnRequestsQueryHandler
    : IRequestHandler<GetMyReturnRequestsQuery, PagedResult<ReturnRequestSummaryDto>>
{
    private readonly IReturnRequestRepository _returnRequestRepository;
    private readonly ICurrentUserService _currentUser;

    // Burada kullanıcının yalnız kendi iade kayıtlarını çözmek için repository ve kimlik servisini hazırlıyorum.
    public GetMyReturnRequestsQueryHandler(
        IReturnRequestRepository returnRequestRepository,
        ICurrentUserService currentUser)
    {
        _returnRequestRepository = returnRequestRepository;
        _currentUser = currentUser;
    }

    // Burada owner filtresini zorunlu aktararak kullanıcının iade özetlerini sayfalıyorum.
    public async Task<PagedResult<ReturnRequestSummaryDto>> Handle(
        GetMyReturnRequestsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetRequiredUserId();
        var returnRequests = await _returnRequestRepository.GetListForUserAsync(
            new ReturnRequestListFilter(
                request.PageNumber,
                request.PageSize,
                Type: request.Type,
                Status: request.Status,
                CreatedFromUtc: request.CreatedFromUtc,
                CreatedToUtc: request.CreatedToUtc),
            userId,
            cancellationToken);
        return returnRequests.Map(returnRequest => returnRequest.ToSummaryDto());
    }
}
