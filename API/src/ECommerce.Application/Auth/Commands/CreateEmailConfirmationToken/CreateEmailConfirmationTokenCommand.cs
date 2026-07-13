using ECommerce.Application.Auth.Dtos;
using MediatR;

namespace ECommerce.Application.Auth.Commands.CreateEmailConfirmationToken;

public sealed record CreateEmailConfirmationTokenCommand(Guid UserId) : IRequest<SecurityTokenResultDto>;
