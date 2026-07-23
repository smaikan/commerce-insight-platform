using ECommerce.Application.Users.Dtos;
using MediatR;

namespace ECommerce.Application.Users.Commands.UpdateProfile;

public sealed record UpdateProfileCommand(
    string FirstName,
    string LastName,
    string? PhoneNumber = null) : IRequest<UserDto>;
