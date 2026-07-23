using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Security;
using MediatR;

namespace ECommerce.Application.Users.Commands.CloseAccount;

public sealed class CloseAccountCommandHandler : IRequestHandler<CloseAccountCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public CloseAccountCommandHandler(
        IUserRepository userRepository,
        ICurrentUserService currentUserService,
        IPasswordHasher passwordHasher,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _currentUserService = currentUserService;
        _passwordHasher = passwordHasher;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    // Burada parolayı doğrulayıp hesabı kapatıyor ve tüm aktif oturumları iptal ediyorum.
    public async Task Handle(CloseAccountCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var user = await _userRepository.GetByIdForUpdateAsync(userId, cancellationToken)
            ?? throw new NotFoundException("User was not found.");

        if (!_passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
        {
            throw new UnauthorizedException("Current password is invalid.");
        }

        var utcNow = _dateTimeProvider.UtcNow;
        var activeTokens = await _userRepository.GetActiveRefreshTokensForUpdateAsync(
            user.Id,
            utcNow,
            cancellationToken);

        foreach (var token in activeTokens)
        {
            token.Revoke(utcNow);
        }

        user.MarkAsDeleted();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
