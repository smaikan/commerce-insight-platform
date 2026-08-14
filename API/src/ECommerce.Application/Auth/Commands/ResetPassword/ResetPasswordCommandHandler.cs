using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Security;
using ECommerce.Domain.Enums;
using MediatR;

namespace ECommerce.Application.Auth.Commands.ResetPassword;

public sealed class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenHasher _tokenHasher;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    // Burada parola sıfırlama işleminin doğrulama ve kalıcılık bağımlılıklarını hazırlıyorum.
    public ResetPasswordCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ITokenHasher tokenHasher,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenHasher = tokenHasher;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    // Burada şifre sıfırlama tokenını doğrulayıp kullanıcı şifresini yeniliyorum.
    public async Task Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = _tokenHasher.Hash(request.Token);
        var securityToken = await _userRepository.GetSecurityTokenForUpdateAsync(
            UserSecurityTokenType.PasswordReset,
            tokenHash,
            cancellationToken);

        var utcNow = _dateTimeProvider.UtcNow;

        if (securityToken is null ||
            securityToken.User.Status != UserStatus.Active ||
            !securityToken.CanBeUsed(utcNow))
        {
            throw new InvalidPasswordResetTokenException();
        }

        var activeRefreshTokens = await _userRepository.GetActiveRefreshTokensForUpdateAsync(
            securityToken.UserId,
            utcNow,
            cancellationToken);

        securityToken.User.ChangePassword(_passwordHasher.Hash(request.NewPassword), utcNow);
        securityToken.MarkAsUsed(utcNow);

        foreach (var refreshToken in activeRefreshTokens)
        {
            refreshToken.Revoke(utcNow);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
