using ECommerce.Application.Common.Security;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Persistence;
using ECommerce.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.API.Configuration;

public static class DatabaseSeeder
{
    public static async Task SeedDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseSeeder");

        var enableSeed = configuration.GetValue<bool>("ENABLE_DEVELOPMENT_SEED");
        if (!enableSeed)
        {
            logger.LogInformation("Development seed is disabled.");
            return;
        }

        var adminEmail = configuration.GetValue<string>("SEED_ADMIN_EMAIL");
        var adminPassword = configuration.GetValue<string>("SEED_ADMIN_PASSWORD");

        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
        {
            logger.LogWarning("ENABLE_DEVELOPMENT_SEED is true, but SEED_ADMIN_EMAIL or SEED_ADMIN_PASSWORD is not configured. Seeding skipped.");
            return;
        }

        try
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            // Veritabanının oluşturulduğundan/güncel olduğundan emin olalım
            await dbContext.Database.MigrateAsync();

            var adminExists = await dbContext.Users.AnyAsync(u => u.Role == UserRole.Admin);
            
            if (adminExists)
            {
                logger.LogInformation("An admin user already exists. Initial seed is skipped.");
                return;
            }

            var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            var passwordHash = passwordHasher.Hash(adminPassword);

            var adminUser = new User(
                email: adminEmail,
                passwordHash: passwordHash,
                firstName: "System",
                lastName: "Admin",
                phoneNumber: null,
                role: UserRole.Admin);

            dbContext.Users.Add(adminUser);
            await dbContext.SaveChangesAsync();

            logger.LogInformation("Successfully seeded the default admin user with email {AdminEmail}", adminEmail);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database.");
            throw; // Fail fast during startup if seeding fails
        }
    }
}
