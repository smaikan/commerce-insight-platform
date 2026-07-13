using MediatR;

namespace ECommerce.Application.Auth.Commands.Logout;

public sealed record LogoutCommand(
    string RefreshToken,
    string? IpAddress = null) : IRequest;
