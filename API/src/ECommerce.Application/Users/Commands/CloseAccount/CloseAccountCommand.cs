using MediatR;

namespace ECommerce.Application.Users.Commands.CloseAccount;

public sealed record CloseAccountCommand(string CurrentPassword) : IRequest;
