using ECommerce.Application.Auth.Dtos;
using MediatR;

namespace ECommerce.Application.Auth.Commands.RegisterUser;

public sealed record RegisterUserCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string? PhoneNumber = null) : IRequest<RegisterUserResultDto>;
