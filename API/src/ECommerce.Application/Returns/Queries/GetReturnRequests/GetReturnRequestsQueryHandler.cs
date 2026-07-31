using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Application.Returns.Dtos;
using MediatR;

namespace ECommerce.Application.Returns.Queries.GetReturnRequests;

public sealed class GetReturnRequestsQueryHandler
    : IRequestHandler<GetReturnRequestsQuery, PagedResult<ReturnRequestSummaryDto>>
{
    private readonly IReturnRequestRepository _returnRequestRepository;

    // Burada yönetim iade listesini çözmek için repository bağımlılığını hazırlıyorum.
    public GetReturnRequestsQueryHandler(IReturnRequestRepository returnRequestRepository)
    {
        _returnRequestRepository = returnRequestRepository;
    }

    // Burada yönetim için iade taleplerini güvenli filtrelerle sayfalayıp özetliyorum.
    public async Task<PagedResult<ReturnRequestSummaryDto>> Handle(
        GetReturnRequestsQuery request,
        CancellationToken cancellationToken)
    {
        var returnRequests = await _returnRequestRepository.GetListAsync(
            new ReturnRequestListFilter(
                request.PageNumber,
                request.PageSize,
                OrderId: request.OrderId,
                Type: request.Type,
                Status: request.Status,
                CreatedFromUtc: request.CreatedFromUtc,
                CreatedToUtc: request.CreatedToUtc),
            cancellationToken);
        return returnRequests.Map(returnRequest => returnRequest.ToSummaryDto());
    }
}
