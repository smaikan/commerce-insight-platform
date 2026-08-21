using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Persistence.Configurations;

public sealed class CollectionConfiguration : IEntityTypeConfiguration<Collection>
{
    // Burada koleksiyon tablosunun alan ve indeks kurallarını tanımlıyorum.
    public void Configure(EntityTypeBuilder<Collection> builder)
    {
        builder.ToTable("Collections", table => table.UseSqlOutputClause(false));

        builder.HasKey(collection => collection.Id);

        builder.Property(collection => collection.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(collection => collection.Description)
            .HasMaxLength(1000);

        builder.Property(collection => collection.ImageUrl)
            .HasMaxLength(500);

        builder.Property(collection => collection.Url)
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(collection => collection.Url)
            .IsUnique();

        builder.HasIndex(collection => collection.DisplayOrder);
    }
}
