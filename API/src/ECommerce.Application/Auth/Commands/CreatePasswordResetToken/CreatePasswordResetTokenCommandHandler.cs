using ECommerce.Application.Auth.Dtos;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Security;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using MediatR;

namespace ECommerce.Application.Auth.Commands.CreatePasswordResetToken;

public sealed class CreatePasswordResetTokenCommandHandler : IRequestHandler<CreatePasswordResetTokenCommand, SecurityTokenResultDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IRandomTokenGenerator _randomTokenGenerator;
    private readonly ITokenHasher _tokenHasher;
    private readonly IAuthSettingsProvider _authSettingsProvider;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public CreatePasswordResetTokenCommandHandler(
        IUserRepository userRepository,
        IRandomTokenGenerator randomTokenGenerator,
        ITokenHasher tokenHasher,
        IAuthSettingsProvider authSettingsProvider,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _randomTokenGenerator = randomTokenGenerator;
        _tokenHasher = tokenHasher;
        _authSettingsProvider = authSettingsProvider;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    // Burada şifre sıfırlama için tek kullanımlık ve süreli token hazırlıyorum.
    public async Task<SecurityTokenResultDto> Handle(CreatePasswordResetTokenCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailForUpdateAsync(request.Email, cancellationToken);

        if (user is null)
        {
            throw new NotFoundException("User was not found.");
        }

        var rawToken = _randomTokenGenerator.GenerateToken();
        var expiresAt = _dateTimeProvider.UtcNow.AddMinutes(_authSettingsProvider.GetSettings().PasswordResetTokenMinutes);

        user.SecurityTokens.Add(new UserSecurityToken(
            user.Id,
            UserSecurityTokenType.PasswordReset,
            _tokenHasher.Hash(rawToken),
            expiresAt));

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new SecurityTokenResultDto(user.Id, rawToken, expiresAt);
    }
}
