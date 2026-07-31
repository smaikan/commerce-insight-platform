using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Persistence.Configurations;

public sealed class VariantOptionValueConfiguration : IEntityTypeConfiguration<VariantOptionValue>
{
    // Burada varyant değerlerinin bağlı ada göre benzersizliğini ve foreign key ilişkisini tanımlıyorum.
    public void Configure(EntityTypeBuilder<VariantOptionValue> builder)
    {
        builder.ToTable("VariantOptionValues");
        builder.HasKey(optionValue => optionValue.Id);
        builder.Property(optionValue => optionValue.Value)
            .HasMaxLength(150)
            .IsRequired();
        builder.HasOne(optionValue => optionValue.VariantOptionName)
            .WithMany(optionName => optionName.Values)
            .HasForeignKey(optionValue => optionValue.VariantOptionNameId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(optionValue => new { optionValue.VariantOptionNameId, optionValue.Value })
            .IsUnique()
            .HasDatabaseName("UX_VariantOptionValues_NameId_Value");
    }
}
