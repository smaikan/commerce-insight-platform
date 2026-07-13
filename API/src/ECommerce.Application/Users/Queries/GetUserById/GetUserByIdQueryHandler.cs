using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Users.Dtos;
using MediatR;

namespace ECommerce.Application.Users.Queries.GetUserById;

public sealed class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, UserDto>
{
    private readonly IUserRepository _userRepository;

    public GetUserByIdQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    // Burada kullanıcıyı okuyup dışarıya güvenli kullanıcı cevabı olarak dönüyorum.
    public async Task<UserDto> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.Id, cancellationToken);

        if (user is null)
        {
            throw new NotFoundException("User was not found.");
        }

        return user.ToDto();
    }
}
