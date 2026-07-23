using ECommerce.Application.Auth.Dtos;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Security;
using ECommerce.Application.Users.Dtos;
using ECommerce.Domain.Entities;
using MediatR;

namespace ECommerce.Application.Auth.Commands.Login;

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResultDto>
{
    private const string DummyPasswordHash = "PBKDF2-SHA256.210000.AAAAAAAAAAAAAAAAAAAAAA.EhL9m-kU-EX8qIcIO_fuNwLe_soCpvv3j9Fbc3tuY0s";
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IRandomTokenGenerator _randomTokenGenerator;
    private readonly ITokenHasher _tokenHasher;
    private readonly IAuthSettingsProvider _authSettingsProvider;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public LoginCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IRandomTokenGenerator randomTokenGenerator,
        ITokenHasher tokenHasher,
        IAuthSettingsProvider authSettingsProvider,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _randomTokenGenerator = randomTokenGenerator;
        _tokenHasher = tokenHasher;
        _authSettingsProvider = authSettingsProvider;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    // Burada email onayı aramadan parolayı doğrulayıp güvenli oturum tokenlarını üretiyorum.
    public async Task<AuthResultDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailForUpdateAsync(request.Email, cancellationToken);

        if (user is null)
        {
            _passwordHasher.Verify(request.Password, DummyPasswordHash);
            throw new UnauthorizedException("Email or password is invalid.");
        }

        var utcNow = _dateTimeProvider.UtcNow;
        var passwordIsValid = _passwordHasher.Verify(request.Password, user.PasswordHash);

        if (!passwordIsValid || !user.CanLogin())
        {
            throw new UnauthorizedException("Email or password is invalid.");
        }

        var settings = _authSettingsProvider.GetSettings();

        if (_passwordHasher.NeedsRehash(user.PasswordHash))
        {
            user.UpgradePasswordHash(_passwordHasher.Hash(request.Password));
        }

        user.RecordSuccessfulLogin(utcNow);
        var tokens = CreateTokens(user, settings, request.IpAddress, request.DeviceName);
        await _userRepository.AddRefreshTokenAsync(tokens.RefreshTokenEntity, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResultDto(user.ToDto(), tokens.Tokens);
    }

    private CreatedTokens CreateTokens(
        User user,
        AuthSettings settings,
        string? ipAddress,
        string? deviceName)
    {
        var refreshToken = _randomTokenGenerator.GenerateToken();
        var refreshTokenExpiresAt = _dateTimeProvider.UtcNow.AddDays(settings.RefreshTokenDays);
        var refreshTokenEntity = new UserRefreshToken(
            user.Id,
            _tokenHasher.Hash(refreshToken),
            refreshTokenExpiresAt,
            _dateTimeProvider.UtcNow,
            ipAddress,
            deviceName);
        user.RefreshTokens.Add(refreshTokenEntity);
        var accessToken = _jwtTokenGenerator.GenerateAccessToken(user, refreshTokenEntity.Id);

        return new CreatedTokens(
            new AuthTokensDto(
                accessToken.Token,
                accessToken.ExpiresAt,
                refreshToken,
                refreshTokenExpiresAt),
            refreshTokenEntity);
    }

    private sealed record CreatedTokens(AuthTokensDto Tokens, UserRefreshToken RefreshTokenEntity);
}
