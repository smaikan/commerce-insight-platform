using ECommerce.Application.Auth.Dtos;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Security;
using ECommerce.Application.Users.Dtos;
using ECommerce.Domain.Entities;
using MediatR;

namespace ECommerce.Application.Auth.Commands.RegisterUser;

public sealed class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, RegisterUserResultDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailOutboxRepository _emailOutboxRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    // Burada kullanıcı kaydıyla hoş geldin e-postası kuyruğunu aynı işlem kapsamında hazırlıyorum.
    public RegisterUserCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IEmailOutboxRepository emailOutboxRepository,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _emailOutboxRepository = emailOutboxRepository;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    // Burada kullanıcıyı oluşturup hoş geldin e-postasını SMTP beklemeden kuyruğa alıyorum.
    public async Task<RegisterUserResultDto> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        if (await _userRepository.EmailExistsAsync(normalizedEmail, cancellationToken: cancellationToken))
        {
            throw new ConflictException("User email already exists.");
        }

        var user = new User(
            normalizedEmail,
            _passwordHasher.Hash(request.Password),
            request.FirstName,
            request.LastName,
            request.PhoneNumber);

        await _userRepository.AddAsync(user, cancellationToken);
        await _emailOutboxRepository.AddAsync(
            EmailOutboxMessage.CreateWelcome(user.Email, user.FullName, _dateTimeProvider.UtcNow),
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new RegisterUserResultDto(user.ToDto());
    }
}
