using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Persistence.Configurations;

public sealed class ProductUrlRedirectConfiguration : IEntityTypeConfiguration<ProductUrlRedirect>
{
    public void Configure(EntityTypeBuilder<ProductUrlRedirect> builder)
    {
        builder.ToTable("ProductUrlRedirects");
        builder.HasKey(redirect => redirect.Id);

        builder.Property(redirect => redirect.Url)
            .HasMaxLength(250)
            .IsRequired();

        builder.HasIndex(redirect => redirect.Url)
            .IsUnique();

        builder.HasIndex(redirect => redirect.ProductId);
    }
}
