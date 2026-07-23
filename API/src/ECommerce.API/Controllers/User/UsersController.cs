using ECommerce.API.Security;
using ECommerce.API.Routing;
using ECommerce.Application.Users.Commands.ChangeEmail;
using ECommerce.Application.Users.Commands.ChangePassword;
using ECommerce.Application.Users.Commands.CloseAccount;
using ECommerce.Application.Users.Commands.LogoutAllSessions;
using ECommerce.Application.Users.Commands.RevokeSession;
using ECommerce.Application.Users.Commands.SetUserRole;
using ECommerce.Application.Users.Commands.SetUserStatus;
using ECommerce.Application.Users.Commands.UpdateProfile;
using ECommerce.Application.Users.Dtos;
using ECommerce.Application.Users.Queries.GetActiveSessions;
using ECommerce.Application.Users.Queries.GetCurrentUser;
using ECommerce.Application.Users.Queries.GetUserById;
using ECommerce.Application.Users.Queries.GetUsers;
using ECommerce.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers.User;

[ApiController]
[Authorize]
[Route("api/users")]
public sealed class UsersController : ControllerBase
{
    private readonly ISender _sender;
    public UsersController(ISender sender) => _sender = sender;

    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> GetCurrentUser(CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new GetCurrentUserQuery(), cancellationToken));

    [HttpPut("me/profile")]
    public async Task<ActionResult<UserDto>> UpdateProfile(
        UpdateProfileCommand command,
        CancellationToken cancellationToken) =>
        Ok(await _sender.Send(command, cancellationToken));

    [HttpPut("me/email")]
    public async Task<ActionResult<UserDto>> ChangeEmail(
        ChangeEmailCommand command,
        CancellationToken cancellationToken) =>
        Ok(await _sender.Send(command, cancellationToken));

    [HttpPut("me/password")]
    public async Task<IActionResult> ChangePassword(
        ChangePasswordCommand command,
        CancellationToken cancellationToken)
    {
        await _sender.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpDelete("me")]
    public async Task<IActionResult> CloseAccount(CloseAccountCommand command, CancellationToken cancellationToken)
    {
        await _sender.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpGet("me/sessions")]
    public async Task<ActionResult<IReadOnlyList<UserSessionDto>>> GetSessions(CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new GetActiveSessionsQuery(), cancellationToken));

    [HttpDelete("me/sessions/{sessionId:guid}")]
    public async Task<IActionResult> RevokeSession(Guid sessionId, CancellationToken cancellationToken)
    {
        await _sender.Send(new RevokeSessionCommand(sessionId), cancellationToken);
        return NoContent();
    }

    [HttpDelete("me/sessions")]
    public async Task<IActionResult> LogoutAllSessions(CancellationToken cancellationToken)
    {
        await _sender.Send(new LogoutAllSessionsCommand(), cancellationToken);
        return NoContent();
    }

    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [HttpGet]
    public async Task<ActionResult> GetUsers([FromQuery] GetUsersQuery query, CancellationToken cancellationToken) =>
        Ok(await _sender.Send(query, cancellationToken));

    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetUserById(string id, CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new GetUserByIdQuery(ApiPublicIdParser.ParseUserId(id)), cancellationToken));

    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [HttpPatch("{id}/role")]
    public async Task<ActionResult<AdminUserDto>> SetRole(
        string id,
        SetUserRoleRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new SetUserRoleCommand(ApiPublicIdParser.ParseUserId(id), request.Role), cancellationToken));

    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [HttpPatch("{id}/status")]
    public async Task<ActionResult<AdminUserDto>> SetStatus(
        string id,
        SetUserStatusRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new SetUserStatusCommand(ApiPublicIdParser.ParseUserId(id), request.Status), cancellationToken));
}

public sealed record SetUserRoleRequest(UserRole Role);
public sealed record SetUserStatusRequest(UserStatus Status);
