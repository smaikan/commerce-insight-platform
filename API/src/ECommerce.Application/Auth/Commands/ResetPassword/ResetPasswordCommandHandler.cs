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

        if (securityToken is null || !securityToken.CanBeUsed(_dateTimeProvider.UtcNow))
        {
            throw new UnauthorizedException("Password reset token is invalid.");
        }

        securityToken.User.ChangePassword(_passwordHasher.Hash(request.NewPassword));
        securityToken.MarkAsUsed(_dateTimeProvider.UtcNow);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
