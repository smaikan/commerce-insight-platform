using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Security;
using MediatR;

namespace ECommerce.Application.Users.Commands.LogoutAllSessions;

public sealed class LogoutAllSessionsCommandHandler : IRequestHandler<LogoutAllSessionsCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public LogoutAllSessionsCommandHandler(
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

    // Burada kullanıcıya ait tüm aktif refresh tokenlarını iptal ediyorum.
    public async Task Handle(LogoutAllSessionsCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var user = await _userRepository.GetByIdForUpdateAsync(userId, cancellationToken)
            ?? throw new NotFoundException("User was not found.");
        var utcNow = _dateTimeProvider.UtcNow;
        var activeTokens = await _userRepository.GetActiveRefreshTokensForUpdateAsync(
            user.Id,
            utcNow,
            cancellationToken);

        foreach (var token in activeTokens)
        {
            token.Revoke(utcNow);
        }

        user.InvalidateAccessTokens();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
