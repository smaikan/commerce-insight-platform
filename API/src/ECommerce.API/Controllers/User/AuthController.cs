using ECommerce.Application.Auth.Commands.CreatePasswordResetToken;
using ECommerce.Application.Auth.Commands.Login;
using ECommerce.Application.Auth.Commands.Logout;
using ECommerce.Application.Auth.Commands.RefreshToken;
using ECommerce.Application.Auth.Commands.RegisterUser;
using ECommerce.Application.Auth.Commands.ResetPassword;
using ECommerce.Application.Auth.Dtos;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers.User;

[ApiController]
[AllowAnonymous]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly ISender _sender;
    public AuthController(ISender sender) => _sender = sender;

    [HttpPost("register")]
    public async Task<ActionResult<RegisterUserResultDto>> Register(
        RegisterUserCommand command,
        CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await _sender.Send(command, cancellationToken));

    [HttpPost("login")]
    public async Task<ActionResult<AuthResultDto>> Login(LoginRequest request, CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new LoginCommand(
            request.Email, request.Password, GetClientIpAddress(), request.DeviceName), cancellationToken));

    [HttpPost("refresh-token")]
    public async Task<ActionResult<AuthResultDto>> RefreshToken(
        RefreshTokenRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new RefreshTokenCommand(
            request.RefreshToken, GetClientIpAddress(), request.DeviceName), cancellationToken));

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(LogoutRequest request, CancellationToken cancellationToken)
    {
        await _sender.Send(new LogoutCommand(request.RefreshToken, GetClientIpAddress()), cancellationToken);
        return NoContent();
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(
        CreatePasswordResetTokenCommand command,
        CancellationToken cancellationToken)
    {
        await _sender.Send(command, cancellationToken);
        return Accepted();
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordCommand command, CancellationToken cancellationToken)
    {
        await _sender.Send(command, cancellationToken);
        return NoContent();
    }

    private string? GetClientIpAddress() => HttpContext.Connection.RemoteIpAddress?.ToString();
}

public sealed record LoginRequest(string Email, string Password, string? DeviceName = null);
public sealed record RefreshTokenRequest(string RefreshToken, string? DeviceName = null);
public sealed record LogoutRequest(string RefreshToken);
