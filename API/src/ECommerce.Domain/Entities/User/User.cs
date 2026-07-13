using ECommerce.Domain.Common;
using ECommerce.Domain.Enums;

namespace ECommerce.Domain.Entities;

public sealed class User : AuditableEntity
{
    public string Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public string? PhoneNumber { get; private set; }
    public UserRole Role { get; private set; }
    public UserStatus Status { get; private set; }
    public bool EmailConfirmed { get; private set; }
    public int AccessFailedCount { get; private set; }
    public DateTime? LockoutEndAt { get; private set; }
    public DateTime? LastLoginAt { get; private set; }
    public DateTime? PasswordChangedAt { get; private set; }

    public ICollection<Address> Addresses { get; private set; } = new List<Address>();
    public ICollection<Cart> Carts { get; private set; } = new List<Cart>();
    public ICollection<CouponUsage> CouponUsages { get; private set; } = new List<CouponUsage>();
    public ICollection<FavoriteProduct> FavoriteProducts { get; private set; } = new List<FavoriteProduct>();
    public ICollection<Order> Orders { get; private set; } = new List<Order>();
    public ICollection<ProductRating> ProductRatings { get; private set; } = new List<ProductRating>();
    public ICollection<ProductReview> ProductReviews { get; private set; } = new List<ProductReview>();
    public ICollection<UserRefreshToken> RefreshTokens { get; private set; } = new List<UserRefreshToken>();
    public ICollection<UserSecurityToken> SecurityTokens { get; private set; } = new List<UserSecurityToken>();

    private User()
    {
    }

    public User(
        string email,
        string passwordHash,
        string firstName,
        string lastName,
        string? phoneNumber = null,
        UserRole role = UserRole.Customer,
        bool emailConfirmed = false)
    {
        SetEmail(email);
        SetPasswordHash(passwordHash);
        SetName(firstName, lastName);
        SetPhoneNumber(phoneNumber);
        Role = role;
        Status = UserStatus.Active;
        EmailConfirmed = emailConfirmed;
    }

    public string FullName => $"{FirstName} {LastName}";

    public bool IsLocked(DateTime utcNow)
    {
        return LockoutEndAt.HasValue && LockoutEndAt.Value > utcNow;
    }

    public bool CanLogin(DateTime utcNow)
    {
        return Status == UserStatus.Active && EmailConfirmed && !IsLocked(utcNow);
    }

    public void ChangeEmail(string email)
    {
        SetEmail(email);
        EmailConfirmed = false;
        MarkAsUpdated();
    }

    public void ConfirmEmail()
    {
        EmailConfirmed = true;
        MarkAsUpdated();
    }

    public void ChangePassword(string passwordHash)
    {
        SetPasswordHash(passwordHash);
        PasswordChangedAt = DateTime.UtcNow;
        AccessFailedCount = 0;
        LockoutEndAt = null;
        MarkAsUpdated();
    }

    public void UpdateProfile(string firstName, string lastName, string? phoneNumber = null)
    {
        SetName(firstName, lastName);
        SetPhoneNumber(phoneNumber);
        MarkAsUpdated();
    }

    public void ChangeRole(UserRole role)
    {
        Role = role;
        MarkAsUpdated();
    }

    public void Activate()
    {
        if (Status == UserStatus.Deleted)
        {
            throw new DomainException("Deleted users cannot be activated.");
        }

        Status = UserStatus.Active;
        LockoutEndAt = null;
        MarkAsUpdated();
    }

    public void Deactivate()
    {
        Status = UserStatus.Passive;
        MarkAsUpdated();
    }

    public void MarkAsDeleted()
    {
        Status = UserStatus.Deleted;
        MarkAsUpdated();
    }

    public void RecordSuccessfulLogin(DateTime utcNow)
    {
        if (Status == UserStatus.Deleted || Status == UserStatus.Passive)
        {
            throw new DomainException("Inactive users cannot login.");
        }

        if (IsLocked(utcNow))
        {
            throw new DomainException("Locked users cannot login before lockout ends.");
        }

        LastLoginAt = utcNow;
        AccessFailedCount = 0;
        LockoutEndAt = null;

        MarkAsUpdated();
    }

    public void RecordFailedLogin(int maxFailedAccessAttempts, TimeSpan lockoutDuration, DateTime utcNow)
    {
        if (maxFailedAccessAttempts <= 0)
        {
            throw new DomainException("Max failed access attempts must be greater than zero.");
        }

        if (lockoutDuration <= TimeSpan.Zero)
        {
            throw new DomainException("Lockout duration must be greater than zero.");
        }

        if (Status == UserStatus.Deleted || Status == UserStatus.Passive)
        {
            throw new DomainException("Inactive users cannot login.");
        }

        AccessFailedCount++;

        if (AccessFailedCount >= maxFailedAccessAttempts)
        {
            LockUntil(utcNow.Add(lockoutDuration), utcNow);
        }
        else
        {
            MarkAsUpdated();
        }
    }

    public void LockUntil(DateTime lockoutEndAt)
    {
        LockUntil(lockoutEndAt, DateTime.UtcNow);
    }

    private void LockUntil(DateTime lockoutEndAt, DateTime utcNow)
    {
        if (lockoutEndAt <= utcNow)
        {
            throw new DomainException("Lockout end date must be in the future.");
        }

        LockoutEndAt = lockoutEndAt;
        MarkAsUpdated();
    }

    public void Unlock()
    {
        if (Status == UserStatus.Deleted)
        {
            throw new DomainException("Deleted users cannot be unlocked.");
        }

        AccessFailedCount = 0;
        LockoutEndAt = null;
        MarkAsUpdated();
    }

    private void SetEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new DomainException("User email cannot be empty.");
        }

        var normalizedEmail = email.Trim().ToLowerInvariant();

        if (!normalizedEmail.Contains('@', StringComparison.Ordinal) || normalizedEmail.Length < 5)
        {
            throw new DomainException("User email is not valid.");
        }

        Email = normalizedEmail;
    }

    private void SetPasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new DomainException("Password hash cannot be empty.");
        }

        PasswordHash = passwordHash.Trim();
    }

    private void SetName(string firstName, string lastName)
    {
        if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
        {
            throw new DomainException("User first and last name cannot be empty.");
        }

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
    }

    private void SetPhoneNumber(string? phoneNumber)
    {
        PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber)
            ? null
            : phoneNumber.Trim();
    }
}
