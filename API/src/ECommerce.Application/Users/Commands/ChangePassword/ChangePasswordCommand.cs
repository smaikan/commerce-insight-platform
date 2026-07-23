using MediatR;

namespace ECommerce.Application.Users.Commands.ChangePassword;

public sealed record ChangePasswordCommand(
    string CurrentPassword,
    string NewPassword) : IRequest;
