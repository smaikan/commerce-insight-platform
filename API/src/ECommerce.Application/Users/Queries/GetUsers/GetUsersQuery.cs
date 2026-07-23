using ECommerce.Application.Common.Models;
using ECommerce.Application.Users.Dtos;
using MediatR;
using ECommerce.Domain.Enums;

namespace ECommerce.Application.Users.Queries.GetUsers;

public sealed record GetUsersQuery(
    int PageNumber = 1,
    int PageSize = 20,
    string? Search = null,
    UserRole? Role = null,
    UserStatus? Status = null) : IRequest<PagedResult<AdminUserDto>>;
