using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Security;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using MediatR;

namespace ECommerce.Application.Auth.Commands.CreatePasswordResetToken;

public sealed class CreatePasswordResetTokenCommandHandler : IRequestHandler<CreatePasswordResetTokenCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IRandomTokenGenerator _randomTokenGenerator;
    private readonly ITokenHasher _tokenHasher;
    private readonly IAuthSettingsProvider _authSettingsProvider;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailOutboxRepository _outboxRepository;
    private readonly IPasswordResetTokenProtector _tokenProtector;

    // Burada parola sıfırlama tokenı ile e-posta outbox bağımlılıklarını hazırlıyorum.
    public CreatePasswordResetTokenCommandHandler(
        IUserRepository userRepository,
        IRandomTokenGenerator randomTokenGenerator,
        ITokenHasher tokenHasher,
        IAuthSettingsProvider authSettingsProvider,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork,
        IEmailOutboxRepository outboxRepository,
        IPasswordResetTokenProtector tokenProtector)
    {
        _userRepository = userRepository;
        _randomTokenGenerator = randomTokenGenerator;
        _tokenHasher = tokenHasher;
        _authSettingsProvider = authSettingsProvider;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
        _outboxRepository = outboxRepository;
        _tokenProtector = tokenProtector;
    }

    // Burada kullanıcı varlığını açığa çıkarmadan parola sıfırlama emailini hazırlıyorum.
    public async Task Handle(CreatePasswordResetTokenCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailForUpdateAsync(request.Email, cancellationToken);

        if (user is null || user.Status != UserStatus.Active)
        {
            return;
        }

        var rawToken = _randomTokenGenerator.GenerateToken();
        var utcNow = _dateTimeProvider.UtcNow;
        var expiresAt = utcNow.AddMinutes(_authSettingsProvider.GetSettings().PasswordResetTokenMinutes);

        var activeTokens = await _userRepository.GetActiveSecurityTokensForUpdateAsync(
            user.Id,
            UserSecurityTokenType.PasswordReset,
            utcNow,
            cancellationToken);

        foreach (var activeToken in activeTokens)
        {
            activeToken.Invalidate(utcNow);
        }

        user.SecurityTokens.Add(new UserSecurityToken(
            user.Id,
            UserSecurityTokenType.PasswordReset,
            _tokenHasher.Hash(rawToken),
            expiresAt,
            utcNow));

        await _outboxRepository.AddAsync(EmailOutboxMessage.CreatePasswordReset(
            user.Email,
            _tokenProtector.Protect(rawToken),
            expiresAt,
            utcNow), cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
