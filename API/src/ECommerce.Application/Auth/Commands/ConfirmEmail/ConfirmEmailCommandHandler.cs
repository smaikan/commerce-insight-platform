using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Security;
using ECommerce.Application.Users.Dtos;
using ECommerce.Domain.Enums;
using MediatR;

namespace ECommerce.Application.Auth.Commands.ConfirmEmail;

public sealed class ConfirmEmailCommandHandler : IRequestHandler<ConfirmEmailCommand, UserDto>
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenHasher _tokenHasher;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public ConfirmEmailCommandHandler(
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

    // Burada email doğrulama tokenını kullanıp kullanıcı emailini onaylıyorum.
    public async Task<UserDto> Handle(ConfirmEmailCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = _tokenHasher.Hash(request.Token);
        var securityToken = await _userRepository.GetSecurityTokenForUpdateAsync(
            UserSecurityTokenType.EmailConfirmation,
            tokenHash,
            cancellationToken);

        if (securityToken is null || !securityToken.CanBeUsed(_dateTimeProvider.UtcNow))
        {
            throw new UnauthorizedException("Email confirmation token is invalid.");
        }

        securityToken.User.ConfirmEmail();
        securityToken.MarkAsUsed(_dateTimeProvider.UtcNow);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return securityToken.User.ToDto();
    }
}
