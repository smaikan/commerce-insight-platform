using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Security;
using MediatR;

namespace ECommerce.Application.Users.Commands.ChangePassword;

public sealed class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public ChangePasswordCommandHandler(
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

    // Burada mevcut parolayı doğrulayıp tüm oturumları kapatarak yeni parolayı kaydediyorum.
    public async Task Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
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

        user.ChangePassword(_passwordHasher.Hash(request.NewPassword), utcNow);

        foreach (var token in activeTokens)
        {
            token.Revoke(utcNow);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
