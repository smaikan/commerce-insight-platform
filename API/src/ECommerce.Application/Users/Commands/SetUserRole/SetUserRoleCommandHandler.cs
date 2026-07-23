using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Users.Dtos;
using ECommerce.Domain.Enums;
using MediatR;

namespace ECommerce.Application.Users.Commands.SetUserRole;

public sealed class SetUserRoleCommandHandler : IRequestHandler<SetUserRoleCommand, AdminUserDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    // Burada rol yönetimi için kullanıcı ve oturum bağımlılıklarını hazırlıyorum.
    public SetUserRoleCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    // Burada kullanıcı rolünü değiştirip mevcut access tokenlarını geçersiz hale getiriyorum.
    public async Task<AdminUserDto> Handle(SetUserRoleCommand request, CancellationToken cancellationToken)
    {
        return await _unitOfWork.ExecuteInSerializableTransactionAsync(async transactionCancellationToken =>
        {
            var user = await _userRepository.GetByIdForUpdateAsync(request.UserId, transactionCancellationToken)
                ?? throw new NotFoundException("User was not found.");

            if (user.Role == UserRole.Admin && request.Role != UserRole.Admin &&
                !await _userRepository.HasAnotherActiveAdminAsync(user.Id, transactionCancellationToken))
            {
                throw new ConflictException("The last active admin role cannot be removed.");
            }

            user.ChangeRole(request.Role);
            await _unitOfWork.SaveChangesAsync(transactionCancellationToken);
            return user.ToAdminDto();
        }, cancellationToken);
    }
}
