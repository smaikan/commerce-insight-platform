using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Security;
using MediatR;

namespace ECommerce.Application.Users.Commands.RevokeSession;

public sealed class RevokeSessionCommandHandler : IRequestHandler<RevokeSessionCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public RevokeSessionCommandHandler(
        IUserRepository userRepository,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    // Burada kullanıcıya ait seçili oturumu güvenli ve tekrar çağrılabilir şekilde kapatıyorum.
    public async Task Handle(RevokeSessionCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var token = await _userRepository.GetRefreshTokenForUpdateAsync(
            userId,
            request.SessionId,
            cancellationToken) ?? throw new NotFoundException("Session was not found.");

        if (!token.IsRevoked())
        {
            token.Revoke(_dateTimeProvider.UtcNow);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
