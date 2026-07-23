using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Security;
using ECommerce.Application.Users.Dtos;
using MediatR;

namespace ECommerce.Application.Users.Commands.ChangeEmail;

public sealed class ChangeEmailCommandHandler : IRequestHandler<ChangeEmailCommand, UserDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ChangeEmailCommandHandler(
        IUserRepository userRepository,
        ICurrentUserService currentUserService,
        IPasswordHasher passwordHasher,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _currentUserService = currentUserService;
        _passwordHasher = passwordHasher;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    // Burada email çakışmasını kontrol ederek kullanıcı emailini doğrudan güncelliyorum.
    public async Task<UserDto> Handle(ChangeEmailCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var user = await _userRepository.GetByIdForUpdateAsync(userId, cancellationToken)
            ?? throw new NotFoundException("User was not found.");

        if (!_passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
        {
            throw new UnauthorizedException("Current password is invalid.");
        }

        if (await _userRepository.EmailExistsAsync(request.NewEmail, user.Id, cancellationToken))
        {
            throw new ConflictException("User email already exists.");
        }

        var utcNow = _dateTimeProvider.UtcNow;
        var activeTokens = await _userRepository.GetActiveRefreshTokensForUpdateAsync(
            user.Id,
            utcNow,
            cancellationToken);

        user.ChangeEmail(request.NewEmail);

        foreach (var token in activeTokens)
        {
            token.Revoke(utcNow);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return user.ToDto();
    }
}
