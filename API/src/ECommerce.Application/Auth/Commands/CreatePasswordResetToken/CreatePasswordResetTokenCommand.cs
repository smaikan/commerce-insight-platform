using MediatR;

namespace ECommerce.Application.Auth.Commands.CreatePasswordResetToken;

public sealed record CreatePasswordResetTokenCommand(string Email) : IRequest;
