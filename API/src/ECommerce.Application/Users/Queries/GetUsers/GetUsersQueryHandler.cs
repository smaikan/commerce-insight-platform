using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Application.Users.Dtos;
using MediatR;

namespace ECommerce.Application.Users.Queries.GetUsers;

public sealed class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, PagedResult<AdminUserDto>>
{
    private readonly IUserRepository _userRepository;

    public GetUsersQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    // Burada kullanıcı listesini yönetim bilgileriyle sayfalı olarak hazırlıyorum.
    public async Task<PagedResult<AdminUserDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        return await _userRepository.GetListAsync(
            new UserListFilter(request.PageNumber, request.PageSize, request.Search, request.Role, request.Status),
            cancellationToken);
    }
}
