using ECommerce.Application.Auth.Dtos;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Security;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using MediatR;

namespace ECommerce.Application.Auth.Commands.CreateEmailConfirmationToken;

public sealed class CreateEmailConfirmationTokenCommandHandler : IRequestHandler<CreateEmailConfirmationTokenCommand, SecurityTokenResultDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IRandomTokenGenerator _randomTokenGenerator;
    private readonly ITokenHasher _tokenHasher;
    private readonly IAuthSettingsProvider _authSettingsProvider;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public CreateEmailConfirmationTokenCommandHandler(
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

    // Burada kullanıcı için tek kullanımlık email doğrulama tokenı hazırlıyorum.
    public async Task<SecurityTokenResultDto> Handle(CreateEmailConfirmationTokenCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdForUpdateAsync(request.UserId, cancellationToken);

        if (user is null)
        {
            throw new NotFoundException("User was not found.");
        }

        if (user.EmailConfirmed)
        {
            throw new ConflictException("User email is already confirmed.");
        }

        var rawToken = _randomTokenGenerator.GenerateToken();
        var expiresAt = _dateTimeProvider.UtcNow.AddHours(_authSettingsProvider.GetSettings().EmailConfirmationTokenHours);

        user.SecurityTokens.Add(new UserSecurityToken(
            user.Id,
            UserSecurityTokenType.EmailConfirmation,
            _tokenHasher.Hash(rawToken),
            expiresAt));

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new SecurityTokenResultDto(user.Id, rawToken, expiresAt);
    }
}
