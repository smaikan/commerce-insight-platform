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

    // Burada giriş isteğinde şifreyi doğrulayıp başarılıysa access ve refresh token üretiyorum.
    public async Task<AuthResultDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailForUpdateAsync(request.Email, cancellationToken);

        if (user is null)
        {
            throw new UnauthorizedException("Email or password is invalid.");
        }

        var settings = _authSettingsProvider.GetSettings();
        var utcNow = _dateTimeProvider.UtcNow;

        if (!user.CanLogin(utcNow))
        {
            throw new UnauthorizedException("User cannot login.");
        }

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            user.RecordFailedLogin(
                settings.MaxFailedAccessAttempts,
                TimeSpan.FromMinutes(settings.LockoutMinutes),
                utcNow);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            throw new UnauthorizedException("Email or password is invalid.");
        }

        user.RecordSuccessfulLogin(utcNow);
        var tokens = CreateTokens(user, settings, request.IpAddress);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResultDto(user.ToDto(), tokens);
    }

    private AuthTokensDto CreateTokens(User user, AuthSettings settings, string? ipAddress)
    {
        var accessToken = _jwtTokenGenerator.GenerateAccessToken(user);
        var refreshToken = _randomTokenGenerator.GenerateToken();
        var refreshTokenExpiresAt = _dateTimeProvider.UtcNow.AddDays(settings.RefreshTokenDays);

        user.RefreshTokens.Add(new UserRefreshToken(
            user.Id,
            _tokenHasher.Hash(refreshToken),
            refreshTokenExpiresAt,
            ipAddress));

        return new AuthTokensDto(
            accessToken.Token,
            accessToken.ExpiresAt,
            refreshToken,
            refreshTokenExpiresAt);
    }
}
