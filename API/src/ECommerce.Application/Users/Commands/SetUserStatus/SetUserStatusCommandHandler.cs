using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Security;
using ECommerce.Application.Users.Dtos;
using ECommerce.Domain.Enums;
using MediatR;

namespace ECommerce.Application.Users.Commands.SetUserStatus;

public sealed class SetUserStatusCommandHandler : IRequestHandler<SetUserStatusCommand, AdminUserDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    // Burada hesap durumu yönetimi için kullanıcı ve oturum bağımlılıklarını hazırlıyorum.
    public SetUserStatusCommandHandler(
        IUserRepository userRepository,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    // Burada kullanıcı durumunu değiştirip pasif veya silinen hesapların oturumlarını kapatıyorum.
    public async Task<AdminUserDto> Handle(SetUserStatusCommand request, CancellationToken cancellationToken)
    {
        return await _unitOfWork.ExecuteInSerializableTransactionAsync(async transactionCancellationToken =>
        {
            var user = await _userRepository.GetByIdForUpdateAsync(request.UserId, transactionCancellationToken)
                ?? throw new NotFoundException("User was not found.");

            if (user.Role == UserRole.Admin && request.Status != UserStatus.Active &&
                !await _userRepository.HasAnotherActiveAdminAsync(user.Id, transactionCancellationToken))
            {
                throw new ConflictException("The last active admin account cannot be deactivated or deleted.");
            }

            switch (request.Status)
            {
                case UserStatus.Active:
                    user.Activate();
                    break;
                case UserStatus.Passive:
                    user.Deactivate();
                    break;
                case UserStatus.Deleted:
                    user.MarkAsDeleted();
                    break;
                default:
                    throw new ConflictException("User status is invalid.");
            }

            if (request.Status != UserStatus.Active)
            {
                var utcNow = _dateTimeProvider.UtcNow;
                var activeTokens = await _userRepository.GetActiveRefreshTokensForUpdateAsync(
                    user.Id,
                    utcNow,
                    transactionCancellationToken);

                foreach (var token in activeTokens)
                {
                    token.Revoke(utcNow);
                }
            }

            await _unitOfWork.SaveChangesAsync(transactionCancellationToken);
            return user.ToAdminDto();
        }, cancellationToken);
    }
}
