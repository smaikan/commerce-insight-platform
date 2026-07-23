using ECommerce.Domain.Common;
using ECommerce.Domain.Enums;

namespace ECommerce.Domain.Entities;

public sealed class User : AuditableEntity<long>
{
    public string Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public string? PhoneNumber { get; private set; }
    public UserRole Role { get; private set; }
    public UserStatus Status { get; private set; }
    public int SecurityVersion { get; private set; }
    public Guid ConcurrencyToken { get; private set; }
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
        UserRole role = UserRole.Customer)
    {
        SetEmail(email);
        SetPasswordHash(passwordHash);
        SetName(firstName, lastName);
        SetPhoneNumber(phoneNumber);
        Role = role;
        Status = UserStatus.Active;
        SecurityVersion = 1;
        ConcurrencyToken = Guid.NewGuid();
    }

    public string FullName => $"{FirstName} {LastName}";

    public bool CanLogin()
    {
        return Status == UserStatus.Active;
    }

    public void ChangeEmail(string email)
    {
        SetEmail(email);
        IncreaseSecurityVersion();
        MarkAsUpdated();
    }

    public void ChangePassword(string passwordHash, DateTime utcNow)
    {
        SetPasswordHash(passwordHash);
        PasswordChangedAt = utcNow;
        IncreaseSecurityVersion();
        MarkAsUpdated();
    }

    public void UpgradePasswordHash(string passwordHash)
    {
        SetPasswordHash(passwordHash);
        ConcurrencyToken = Guid.NewGuid();
        MarkAsUpdated();
    }

    public void UpdateProfile(string firstName, string lastName, string? phoneNumber = null)
    {
        SetName(firstName, lastName);
        SetPhoneNumber(phoneNumber);
        ConcurrencyToken = Guid.NewGuid();
        MarkAsUpdated();
    }

    public void ChangeRole(UserRole role)
    {
        Role = role;
        IncreaseSecurityVersion();
        MarkAsUpdated();
    }

    public void Activate()
    {
        if (Status == UserStatus.Deleted)
        {
            throw new DomainException("Deleted users cannot be activated.");
        }

        Status = UserStatus.Active;
        IncreaseSecurityVersion();
        MarkAsUpdated();
    }

    public void Deactivate()
    {
        Status = UserStatus.Passive;
        IncreaseSecurityVersion();
        MarkAsUpdated();
    }

    public void MarkAsDeleted()
    {
        Status = UserStatus.Deleted;
        IncreaseSecurityVersion();
        MarkAsUpdated();
    }

    public void RecordSuccessfulLogin(DateTime utcNow)
    {
        if (Status == UserStatus.Deleted || Status == UserStatus.Passive)
        {
            throw new DomainException("Inactive users cannot login.");
        }

        LastLoginAt = utcNow;
        MarkAsUpdated();
    }

    public void InvalidateAccessTokens()
    {
        IncreaseSecurityVersion();
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

    private void IncreaseSecurityVersion()
    {
        checked
        {
            SecurityVersion++;
            ConcurrencyToken = Guid.NewGuid();
        }
    }
}
