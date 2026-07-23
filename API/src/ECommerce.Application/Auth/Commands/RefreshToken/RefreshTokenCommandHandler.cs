using ECommerce.Application.Auth.Dtos;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Security;
using ECommerce.Application.Users.Dtos;
using ECommerce.Domain.Entities;
using MediatR;

namespace ECommerce.Application.Auth.Commands.RefreshToken;

public sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResultDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IRandomTokenGenerator _randomTokenGenerator;
    private readonly ITokenHasher _tokenHasher;
    private readonly IAuthSettingsProvider _authSettingsProvider;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public RefreshTokenCommandHandler(
        IUserRepository userRepository,
        IJwtTokenGenerator jwtTokenGenerator,
        IRandomTokenGenerator randomTokenGenerator,
        ITokenHasher tokenHasher,
        IAuthSettingsProvider authSettingsProvider,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _jwtTokenGenerator = jwtTokenGenerator;
        _randomTokenGenerator = randomTokenGenerator;
        _tokenHasher = tokenHasher;
        _authSettingsProvider = authSettingsProvider;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    // Burada refresh tokenı doğrulayıp eski tokenı iptal ederek yeni token çifti üretiyorum.
    public async Task<AuthResultDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var refreshTokenHash = _tokenHasher.Hash(request.RefreshToken);
        var user = await _userRepository.GetByRefreshTokenHashForUpdateAsync(refreshTokenHash, cancellationToken);

        if (user is null)
        {
            throw new UnauthorizedException("Refresh token is invalid.");
        }

        var utcNow = _dateTimeProvider.UtcNow;

        if (!user.CanLogin())
        {
            throw new UnauthorizedException("User cannot refresh token.");
        }

        var oldRefreshToken = user.RefreshTokens.FirstOrDefault(token => token.TokenHash == refreshTokenHash);

        if (oldRefreshToken is null)
        {
            throw new UnauthorizedException("Refresh token is invalid.");
        }

        if (oldRefreshToken.IsRevoked() && !string.IsNullOrWhiteSpace(oldRefreshToken.ReplacedByTokenHash))
        {
            var activeTokens = await _userRepository.GetActiveRefreshTokensForUpdateAsync(
                user.Id,
                utcNow,
                cancellationToken);

            foreach (var activeToken in activeTokens)
            {
                activeToken.Revoke(utcNow, request.IpAddress);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            throw new UnauthorizedException("Refresh token reuse was detected. All sessions were revoked.");
        }

        if (!oldRefreshToken.IsActive(utcNow))
        {
            throw new UnauthorizedException("Refresh token is invalid.");
        }

        var settings = _authSettingsProvider.GetSettings();
        var newRefreshToken = _randomTokenGenerator.GenerateToken();
        var newRefreshTokenHash = _tokenHasher.Hash(newRefreshToken);
        var newRefreshTokenExpiresAt = utcNow.AddDays(settings.RefreshTokenDays);

        oldRefreshToken.Revoke(utcNow, request.IpAddress, newRefreshTokenHash);
        var newRefreshTokenEntity = new UserRefreshToken(
            user.Id,
            newRefreshTokenHash,
            newRefreshTokenExpiresAt,
            utcNow,
            request.IpAddress,
            request.DeviceName);
        user.RefreshTokens.Add(newRefreshTokenEntity);
        await _userRepository.AddRefreshTokenAsync(newRefreshTokenEntity, cancellationToken);
        var accessToken = _jwtTokenGenerator.GenerateAccessToken(user, newRefreshTokenEntity.Id);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResultDto(
            user.ToDto(),
            new AuthTokensDto(accessToken.Token, accessToken.ExpiresAt, newRefreshToken, newRefreshTokenExpiresAt));
    }
}
