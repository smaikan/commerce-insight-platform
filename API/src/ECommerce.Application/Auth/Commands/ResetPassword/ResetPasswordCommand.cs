using MediatR;

namespace ECommerce.Application.Auth.Commands.ResetPassword;

public sealed record ResetPasswordCommand(
    string Token,
    string NewPassword) : IRequest;
