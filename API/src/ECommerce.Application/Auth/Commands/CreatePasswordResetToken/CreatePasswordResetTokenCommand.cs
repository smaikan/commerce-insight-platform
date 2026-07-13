using ECommerce.Application.Auth.Dtos;
using MediatR;

namespace ECommerce.Application.Auth.Commands.CreatePasswordResetToken;

public sealed record CreatePasswordResetTokenCommand(string Email) : IRequest<SecurityTokenResultDto>;
