using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Persistence.Configurations;

public sealed class BrandConfiguration : IEntityTypeConfiguration<Brand>
{
    // Burada marka tablosunun alan ve indeks kurallarını tanımlıyorum.
    public void Configure(EntityTypeBuilder<Brand> builder)
    {
        builder.ToTable("Brands");

        builder.HasKey(brand => brand.Id);

        builder.Property(brand => brand.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(brand => brand.Description)
            .HasMaxLength(1000);

        builder.Property(brand => brand.ImageUrl)
            .HasMaxLength(500);

        builder.Property(brand => brand.Url)
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(brand => brand.Url)
            .IsUnique();

        builder.HasIndex(brand => brand.Name);
    }
}
