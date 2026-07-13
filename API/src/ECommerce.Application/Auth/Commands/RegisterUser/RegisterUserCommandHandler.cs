using ECommerce.Application.Auth.Dtos;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Security;
using ECommerce.Application.Users.Dtos;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using MediatR;

namespace ECommerce.Application.Auth.Commands.RegisterUser;

public sealed class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, RegisterUserResultDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IRandomTokenGenerator _randomTokenGenerator;
    private readonly ITokenHasher _tokenHasher;
    private readonly IAuthSettingsProvider _authSettingsProvider;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public RegisterUserCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IRandomTokenGenerator randomTokenGenerator,
        ITokenHasher tokenHasher,
        IAuthSettingsProvider authSettingsProvider,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _randomTokenGenerator = randomTokenGenerator;
        _tokenHasher = tokenHasher;
        _authSettingsProvider = authSettingsProvider;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    // Burada kullanıcıyı oluşturmadan önce email çakışmasını kontrol edip doğrulama tokenını hazırlıyorum.
    public async Task<RegisterUserResultDto> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        if (await _userRepository.EmailExistsAsync(normalizedEmail, cancellationToken: cancellationToken))
        {
            throw new ConflictException("User email already exists.");
        }

        var user = new User(
            normalizedEmail,
            _passwordHasher.Hash(request.Password),
            request.FirstName,
            request.LastName,
            request.PhoneNumber);

        var settings = _authSettingsProvider.GetSettings();
        var rawToken = _randomTokenGenerator.GenerateToken();
        var tokenExpiresAt = _dateTimeProvider.UtcNow.AddHours(settings.EmailConfirmationTokenHours);

        user.SecurityTokens.Add(new UserSecurityToken(
            user.Id,
            UserSecurityTokenType.EmailConfirmation,
            _tokenHasher.Hash(rawToken),
            tokenExpiresAt));

        await _userRepository.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new RegisterUserResultDto(user.ToDto(), rawToken, tokenExpiresAt);
    }
}
