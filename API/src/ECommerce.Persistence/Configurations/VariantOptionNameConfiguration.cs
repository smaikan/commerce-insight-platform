using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Persistence.Configurations;

public sealed class VariantOptionNameConfiguration : IEntityTypeConfiguration<VariantOptionName>
{
    // Burada merkezi varyant adlarının büyük-küçük harf duyarlı benzersizliğini tanımlıyorum.
    public void Configure(EntityTypeBuilder<VariantOptionName> builder)
    {
        builder.ToTable("VariantOptionNames");
        builder.HasKey(optionName => optionName.Id);
        builder.Property(optionName => optionName.Name)
            .HasMaxLength(150)
            .IsRequired();
        builder.HasIndex(optionName => optionName.Name)
            .IsUnique()
            .HasDatabaseName("UX_VariantOptionNames_Name");
    }
}
