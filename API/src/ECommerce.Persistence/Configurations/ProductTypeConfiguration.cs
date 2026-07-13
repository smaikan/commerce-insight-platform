using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Persistence.Configurations;

public sealed class ProductTypeConfiguration : IEntityTypeConfiguration<ProductType>
{
    public void Configure(EntityTypeBuilder<ProductType> builder)
    {
        builder.ToTable("ProductTypes");

        builder.HasKey(type => type.Id);

        builder.Property(type => type.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(type => type.Description)
            .HasMaxLength(1000);

        builder.HasIndex(type => type.Name)
            .IsUnique();
    }
}
