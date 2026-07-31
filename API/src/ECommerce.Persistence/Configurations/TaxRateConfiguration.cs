using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Persistence.Configurations;

public sealed class TaxRateConfiguration : IEntityTypeConfiguration<TaxRate>
{
    // Burada vergi oranı tablosunun alan, benzersizlik ve değer sınırlarını tanımlıyorum.
    public void Configure(EntityTypeBuilder<TaxRate> builder)
    {
        builder.ToTable("TaxRates", table =>
            table.HasCheckConstraint(
                "CK_TaxRates_Rate_Range",
                $"CAST([Rate] AS REAL) >= {TaxRate.MinimumRate} AND CAST([Rate] AS REAL) <= {TaxRate.MaximumRate}"));

        builder.HasKey(taxRate => taxRate.Id);

        builder.Property(taxRate => taxRate.Name)
            .HasMaxLength(TaxRate.MaximumNameLength)
            .IsRequired();

        builder.Property(taxRate => taxRate.Rate)
            .HasPrecision(5, 2)
            .IsRequired();

        builder.Property(taxRate => taxRate.IsActive)
            .IsRequired();

        builder.HasIndex(taxRate => taxRate.Name)
            .IsUnique();

        builder.HasIndex(taxRate => taxRate.IsActive);
    }
}
