using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Returns.Dtos;
using MediatR;

namespace ECommerce.Application.Returns.Queries.GetReturnRequestById;

public sealed class GetReturnRequestByIdQueryHandler : IRequestHandler<GetReturnRequestByIdQuery, ReturnRequestDto>
{
    private readonly IReturnRequestRepository _returnRequestRepository;
    private readonly ICurrentUserService _currentUser;

    // Burada iade detayını yalnız gerçek sahibi için çözmek üzere repository ve kimlik servisini hazırlıyorum.
    public GetReturnRequestByIdQueryHandler(
        IReturnRequestRepository returnRequestRepository,
        ICurrentUserService currentUser)
    {
        _returnRequestRepository = returnRequestRepository;
        _currentUser = currentUser;
    }

    // Burada kullanıcı kimliğini repository owner filtresine zorunlu geçirerek iade detayını getiriyorum.
    public async Task<ReturnRequestDto> Handle(GetReturnRequestByIdQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetRequiredUserId();
        var returnRequest = await _returnRequestRepository.GetByIdForUserAsync(
            request.ReturnRequestId,
            userId,
            cancellationToken)
            ?? throw new NotFoundException("Return request was not found.");
        return returnRequest.ToDto();
    }
}
