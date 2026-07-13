using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Security;
using MediatR;

namespace ECommerce.Application.Auth.Commands.Logout;

public sealed class LogoutCommandHandler : IRequestHandler<LogoutCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenHasher _tokenHasher;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public LogoutCommandHandler(
        IUserRepository userRepository,
        ITokenHasher tokenHasher,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _tokenHasher = tokenHasher;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    // Burada refresh tokenı bularak oturumu tekrar kullanılamayacak şekilde iptal ediyorum.
    public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var refreshTokenHash = _tokenHasher.Hash(request.RefreshToken);
        var user = await _userRepository.GetByRefreshTokenHashForUpdateAsync(refreshTokenHash, cancellationToken);

        if (user is null)
        {
            throw new UnauthorizedException("Refresh token is invalid.");
        }

        var refreshToken = user.RefreshTokens.FirstOrDefault(token => token.TokenHash == refreshTokenHash);

        if (refreshToken is null || refreshToken.IsRevoked())
        {
            throw new UnauthorizedException("Refresh token is invalid.");
        }

        refreshToken.Revoke(_dateTimeProvider.UtcNow, request.IpAddress);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
