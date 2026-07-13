using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Persistence.Configurations;

public sealed class AddressConfiguration : IEntityTypeConfiguration<Address>
{
    public void Configure(EntityTypeBuilder<Address> builder)
    {
        builder.ToTable("Addresses");

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

        builder.Property(address => address.FullAddress)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(address => address.PostalCode)
            .HasMaxLength(20);

        builder.HasIndex(address => address.UserId);
    }
}
