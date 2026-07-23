using ECommerce.Domain.Enums;

namespace ECommerce.Application.Common.Models;

public sealed record UserListFilter(
    int PageNumber,
    int PageSize,
    string? Search = null,
    UserRole? Role = null,
    UserStatus? Status = null);
