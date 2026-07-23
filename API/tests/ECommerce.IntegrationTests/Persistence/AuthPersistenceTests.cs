using ECommerce.Application.Auth.Commands.ResetPassword;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Security;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Persistence.Context;
using ECommerce.Persistence.Repositories;
using ECommerce.Persistence.Services;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.IntegrationTests.Persistence;

public sealed class AuthPersistenceTests
{
    // Burada parola sıfırlama mesajının yalnızca korumalı tokenla genel e-posta kuyruğunda saklandığını doğruluyorum.
    [Fact]
    public async Task PasswordResetOutbox_Should_Persist_Only_Protected_Token_And_Return_Pending_Message()
    {
        var utcNow = new DateTime(2026, 7, 14, 10, 0, 0, DateTimeKind.Utc);
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();
        var message = EmailOutboxMessage.CreatePasswordReset(
            "user@example.com", "protected-token-value", utcNow.AddMinutes(30), utcNow);
        var repository = new EmailOutboxRepository(context);

        await repository.AddAsync(message);
        await context.SaveChangesAsync();
        var pending = await repository.GetPendingForUpdateAsync(utcNow, 10);

        pending.Should().ContainSingle();
        pending[0].ProtectedToken.Should().Be("protected-token-value");
        pending[0].ProtectedToken.Should().NotBe("raw-reset-token");
    }

    [Fact]
    public async Task TokenCleanup_Should_Delete_Only_Records_Older_Than_Retention_Cutoff()
    {
        var utcNow = new DateTime(2026, 7, 14, 10, 0, 0, DateTimeKind.Utc);
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();
        var user = new User("user@example.com", "password-hash", "User", "Test");
        user.RefreshTokens.Add(new UserRefreshToken(
            user,
            "expired-refresh-hash",
            utcNow.AddDays(-50),
            utcNow.AddDays(-60)));
        user.RefreshTokens.Add(new UserRefreshToken(
            user,
            "active-refresh-hash",
            utcNow.AddDays(1),
            utcNow));
        user.SecurityTokens.Add(new UserSecurityToken(
            user,
            UserSecurityTokenType.PasswordReset,
            "expired-reset-hash",
            utcNow.AddDays(-50),
            utcNow.AddDays(-60)));
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var deletedCount = await new UserTokenCleanupService(context)
            .CleanupAsync(utcNow.AddDays(-30));

        deletedCount.Should().Be(2);
        (await context.UserRefreshTokens.CountAsync()).Should().Be(1);
        (await context.UserSecurityTokens.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task AccessTokenValidation_Should_Require_Active_Session_And_Current_SecurityVersion()
    {
        var utcNow = new DateTime(2026, 7, 14, 10, 0, 0, DateTimeKind.Utc);
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();
        var user = new User("user@example.com", "password-hash", "User", "Test");
        var session = new UserRefreshToken(user, "session-token-hash", utcNow.AddDays(1), utcNow);
        user.RefreshTokens.Add(session);
        context.Users.Add(user);
        await context.SaveChangesAsync();
        var repository = new UserRepository(context);

        var valid = await repository.IsAccessTokenValidAsync(
            user.Id,
            user.SecurityVersion,
            session.Id,
            utcNow.AddMinutes(1));
        session.Revoke(utcNow.AddMinutes(2));
        await context.SaveChangesAsync();
        var revoked = await repository.IsAccessTokenValidAsync(
            user.Id,
            user.SecurityVersion,
            session.Id,
            utcNow.AddMinutes(3));

        valid.Should().BeTrue();
        revoked.Should().BeFalse();
    }

    [Fact]
    public async Task Adding_Refresh_Token_To_Existing_User_Should_Insert_A_New_Session()
    {
        var utcNow = new DateTime(2026, 7, 14, 10, 0, 0, DateTimeKind.Utc);
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        long userId;

        await using (var seedContext = new AppDbContext(options))
        {
            await seedContext.Database.EnsureCreatedAsync();
            var user = new User("user@example.com", "password-hash", "User", "Test");
            seedContext.Users.Add(user);
            await seedContext.SaveChangesAsync();
            userId = user.Id;
        }

        await using (var commandContext = new AppDbContext(options))
        {
            var repository = new UserRepository(commandContext);
            var user = await repository.GetByEmailForUpdateAsync("user@example.com");
            var refreshToken = new UserRefreshToken(
                userId,
                "new-session-token-hash",
                utcNow.AddDays(14),
                utcNow);

            user!.RefreshTokens.Add(refreshToken);
            user.RecordSuccessfulLogin(utcNow);
            await repository.AddRefreshTokenAsync(refreshToken);
            await new UnitOfWork(commandContext).SaveChangesAsync();
        }

        await using var assertionContext = new AppDbContext(options);
        var savedToken = await assertionContext.UserRefreshTokens.SingleAsync();
        savedToken.UserId.Should().Be(userId);
        savedToken.TokenHash.Should().Be("new-session-token-hash");
    }

    [Fact]
    public async Task UnitOfWork_Should_Report_Concurrent_Refresh_Token_Use()
    {
        var utcNow = new DateTime(2026, 7, 14, 10, 0, 0, DateTimeKind.Utc);
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        await using (var seedContext = new AppDbContext(options))
        {
            await seedContext.Database.EnsureCreatedAsync();
            var user = new User("user@example.com", "password-hash", "User", "Test");
            user.RefreshTokens.Add(new UserRefreshToken(
                user,
                "refresh-token-hash",
                utcNow.AddDays(1),
                utcNow));
            seedContext.Users.Add(user);
            await seedContext.SaveChangesAsync();
        }

        await using var firstContext = new AppDbContext(options);
        await using var secondContext = new AppDbContext(options);
        var firstUser = await new UserRepository(firstContext)
            .GetByRefreshTokenHashForUpdateAsync("refresh-token-hash");
        var secondUser = await new UserRepository(secondContext)
            .GetByRefreshTokenHashForUpdateAsync("refresh-token-hash");

        firstUser!.RefreshTokens.Single().Revoke(utcNow.AddMinutes(1));
        await firstContext.SaveChangesAsync();

        secondUser!.RefreshTokens.Single().Revoke(utcNow.AddMinutes(2));
        var act = () => new UnitOfWork(secondContext).SaveChangesAsync();

        await act.Should().ThrowAsync<ConcurrencyException>();
    }

    [Fact]
    public async Task ResetPassword_Should_Revoke_All_Active_Refresh_Tokens()
    {
        var utcNow = new DateTime(2026, 7, 14, 10, 0, 0, DateTimeKind.Utc);
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        long userId;
        await using (var seedContext = new AppDbContext(options))
        {
            await seedContext.Database.EnsureCreatedAsync();
            var user = new User("user@example.com", "old-password-hash", "User", "Test");
            user.SecurityTokens.Add(new UserSecurityToken(
                user,
                UserSecurityTokenType.PasswordReset,
                "reset-token-hash",
                utcNow.AddMinutes(30),
                utcNow));
            user.RefreshTokens.Add(new UserRefreshToken(
                user,
                "active-refresh-hash-1",
                utcNow.AddDays(1),
                utcNow));
            user.RefreshTokens.Add(new UserRefreshToken(
                user,
                "active-refresh-hash-2",
                utcNow.AddDays(2),
                utcNow));
            seedContext.Users.Add(user);
            await seedContext.SaveChangesAsync();
            userId = user.Id;
        }

        await using (var commandContext = new AppDbContext(options))
        {
            var handler = new ResetPasswordCommandHandler(
                new UserRepository(commandContext),
                new FixedPasswordHasher(),
                new FixedTokenHasher(),
                new FixedDateTimeProvider(utcNow.AddMinutes(1)),
                new UnitOfWork(commandContext));

            await handler.Handle(new ResetPasswordCommand("raw-reset-token", "NewPassword123!"), CancellationToken.None);
        }

        await using var assertionContext = new AppDbContext(options);
        var updatedUser = await assertionContext.Users.SingleAsync(user => user.Id == userId);
        var refreshTokens = await assertionContext.UserRefreshTokens
            .Where(token => token.UserId == userId)
            .ToListAsync();
        var securityToken = await assertionContext.UserSecurityTokens
            .SingleAsync(token => token.UserId == userId);

        updatedUser.PasswordHash.Should().Be("new-password-hash");
        updatedUser.PasswordChangedAt.Should().Be(utcNow.AddMinutes(1));
        refreshTokens.Should().OnlyContain(token => token.RevokedAt == utcNow.AddMinutes(1));
        securityToken.UsedAt.Should().Be(utcNow.AddMinutes(1));
    }

    private sealed class FixedPasswordHasher : IPasswordHasher
    {
        public string Hash(string password) => "new-password-hash";

        public bool Verify(string password, string passwordHash) => false;

        public bool NeedsRehash(string passwordHash) => false;
    }

    private sealed class FixedTokenHasher : ITokenHasher
    {
        public string Hash(string token) => "reset-token-hash";
    }

    private sealed class FixedDateTimeProvider : IDateTimeProvider
    {
        public FixedDateTimeProvider(DateTime utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTime UtcNow { get; }
    }
}
