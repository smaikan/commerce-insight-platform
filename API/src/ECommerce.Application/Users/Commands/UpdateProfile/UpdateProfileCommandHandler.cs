using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Users.Dtos;
using MediatR;

namespace ECommerce.Application.Users.Commands.UpdateProfile;

public sealed class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, UserDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public UpdateProfileCommandHandler(
        IUserRepository userRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    // Burada kullanıcının kendi profil bilgilerini güncelliyorum.
    public async Task<UserDto> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var user = await _userRepository.GetByIdForUpdateAsync(userId, cancellationToken)
            ?? throw new NotFoundException("User was not found.");

        user.UpdateProfile(request.FirstName, request.LastName, request.PhoneNumber);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return user.ToDto();
    }
}
