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
using Microsoft.AspNetCore.RateLimiting;

namespace ECommerce.API.Controllers.User;

[ApiController]
[AllowAnonymous]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly ISender _sender;

    // Burada auth isteklerini Application katmanına iletecek göndericiyi hazırlıyorum.
    public AuthController(ISender sender) => _sender = sender;

    // Burada yeni kullanıcı kaydını Application katmanına iletip oluşturulan kullanıcıyı döndürüyorum.
    [HttpPost("register")]
    public async Task<ActionResult<RegisterUserResultDto>> Register(
        RegisterUserCommand command,
        CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await _sender.Send(command, cancellationToken));

    // Burada giriş isteğine istemci bağlamını ekleyip yeni token çiftini döndürüyorum.
    [HttpPost("login")]
    public async Task<ActionResult<AuthResultDto>> Login(LoginRequest request, CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new LoginCommand(
            request.Email, request.Password, GetClientIpAddress(), request.DeviceName), cancellationToken));

    // Burada refresh token rotasyonunu istemci bağlamıyla Application katmanına iletiyorum.
    [HttpPost("refresh-token")]
    public async Task<ActionResult<AuthResultDto>> RefreshToken(
        RefreshTokenRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new RefreshTokenCommand(
            request.RefreshToken, GetClientIpAddress(), request.DeviceName), cancellationToken));

    // Burada mevcut refresh token oturumunu iptal edip boş başarı cevabı döndürüyorum.
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(LogoutRequest request, CancellationToken cancellationToken)
    {
        await _sender.Send(new LogoutCommand(request.RefreshToken, GetClientIpAddress()), cancellationToken);
        return NoContent();
    }

    // Burada kullanıcı varlığını açıklamayan parola sıfırlama isteğini kabul ediyorum.
    [HttpPost("forgot-password")]
    [EnableRateLimiting("password-reset")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ForgotPassword(
        CreatePasswordResetTokenCommand command,
        CancellationToken cancellationToken)
    {
        await _sender.Send(command, cancellationToken);
        return Accepted();
    }

    // Burada tek kullanımlık tokenla parola değişimini çalıştırıp boş başarı cevabı döndürüyorum.
    [HttpPost("reset-password")]
    [EnableRateLimiting("password-reset")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ResetPassword(ResetPasswordCommand command, CancellationToken cancellationToken)
    {
        await _sender.Send(command, cancellationToken);
        return NoContent();
    }

    // Burada oturum denetimi için istemcinin proxy sonrasında çözümlenmiş IP adresini okuyorum.
    private string? GetClientIpAddress() => HttpContext.Connection.RemoteIpAddress?.ToString();
}

public sealed record LoginRequest(string Email, string Password, string? DeviceName = null);
public sealed record RefreshTokenRequest(string RefreshToken, string? DeviceName = null);
public sealed record LogoutRequest(string RefreshToken);
