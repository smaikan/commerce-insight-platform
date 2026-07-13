using ECommerce.Application.Auth.Dtos;
using MediatR;

namespace ECommerce.Application.Auth.Commands.RefreshToken;

public sealed record RefreshTokenCommand(
    string RefreshToken,
    string? IpAddress = null) : IRequest<AuthResultDto>;
