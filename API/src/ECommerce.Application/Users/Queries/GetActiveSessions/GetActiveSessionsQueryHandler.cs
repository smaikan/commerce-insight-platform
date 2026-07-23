using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Security;
using ECommerce.Application.Users.Dtos;
using MediatR;

namespace ECommerce.Application.Users.Queries.GetActiveSessions;

public sealed class GetActiveSessionsQueryHandler : IRequestHandler<GetActiveSessionsQuery, IReadOnlyList<UserSessionDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICurrentUserService _currentUserService;

    public GetActiveSessionsQueryHandler(
        IUserRepository userRepository,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _userRepository = userRepository;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    // Burada kullanıcının aktif oturumlarını hash bilgilerini açmadan listeliyorum.
    public async Task<IReadOnlyList<UserSessionDto>> Handle(
        GetActiveSessionsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetRequiredUserId();

        if (await _userRepository.GetByIdAsync(userId, cancellationToken) is null)
        {
            throw new NotFoundException("User was not found.");
        }

        var sessions = await _userRepository.GetActiveRefreshTokensAsync(
            userId,
            _dateTimeProvider.UtcNow,
            cancellationToken);
        return sessions.Select(token => token.ToSessionDto()).ToList();
    }
}
