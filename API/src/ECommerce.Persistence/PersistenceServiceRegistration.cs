using ECommerce.Application.Common.Interfaces;
using ECommerce.Persistence.Context;
using ECommerce.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Persistence;

public static class PersistenceServiceRegistration
{
    public static IServiceCollection AddPersistenceServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("DefaultConnection connection string is not configured.");
        }

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IProductVariantRepository, ProductVariantRepository>();
        services.AddScoped<IProductImageRepository, ProductImageRepository>();
        services.AddScoped<IProductTypeRepository, ProductTypeRepository>();
        services.AddScoped<IBrandRepository, BrandRepository>();
        services.AddScoped<ICollectionRepository, CollectionRepository>();
        services.AddScoped<ITagRepository, TagRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
