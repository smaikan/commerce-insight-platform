using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Returns.Dtos;
using MediatR;

namespace ECommerce.Application.Returns.Queries.GetReturnRequestByIdForAdmin;

public sealed class GetReturnRequestByIdForAdminQueryHandler
    : IRequestHandler<GetReturnRequestByIdForAdminQuery, ReturnRequestDto>
{
    private readonly IReturnRequestRepository _returnRequestRepository;

    // Burada yönetim iade detayını çözmek için repository bağımlılığını hazırlıyorum.
    public GetReturnRequestByIdForAdminQueryHandler(IReturnRequestRepository returnRequestRepository)
    {
        _returnRequestRepository = returnRequestRepository;
    }

    // Burada yönetim yetkisi API sınırında doğrulanmış iade detayını getiriyorum.
    public async Task<ReturnRequestDto> Handle(
        GetReturnRequestByIdForAdminQuery request,
        CancellationToken cancellationToken)
    {
        var returnRequest = await _returnRequestRepository.GetByIdAsync(request.ReturnRequestId, cancellationToken)
            ?? throw new NotFoundException("Return request was not found.");
        return returnRequest.ToDto();
    }
}
