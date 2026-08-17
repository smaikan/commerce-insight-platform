using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Persistence.Configurations;

public sealed class AddressConfiguration : IEntityTypeConfiguration<Address>
{
    // Burada adres tablosunun alan sınırlarını, owner indekslerini ve tek varsayılan adres kuralını tanımlıyorum.
    public void Configure(EntityTypeBuilder<Address> builder)
    {
        builder.ToTable("Addresses", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint("CK_Addresses_UserId_Positive", "[UserId] > 0");
        });

        builder.HasKey(address => address.Id);

        builder.Property(address => address.Type)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(address => address.Title)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(address => address.FirstName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(address => address.LastName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(address => address.PhoneNumber)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(address => address.City)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(address => address.District)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(address => address.Neighborhood)
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(address => address.FullAddress)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(address => address.PostalCode)
            .HasMaxLength(20);

        builder.HasIndex(address => address.UserId);
        builder.HasIndex(address => new { address.UserId, address.Type, address.IsDefault })
            .HasFilter("[IsDefault] = 1")
            .IsUnique();
    }
}

