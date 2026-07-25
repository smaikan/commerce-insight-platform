using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Persistence.Configurations;

public sealed class OrderAddressSnapshotConfiguration : IEntityTypeConfiguration<OrderAddressSnapshot>
{
    // Burada değişmez sipariş adresi snapshot tablosunun alan uzunluklarını ve tekil ilişkisini tanımlıyorum.
    public void Configure(EntityTypeBuilder<OrderAddressSnapshot> builder)
    {
        builder.ToTable("OrderAddressSnapshots");
        builder.HasKey(snapshot => snapshot.Id);
        builder.Property(snapshot => snapshot.Type).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(snapshot => snapshot.Title).HasMaxLength(OrderAddressSnapshot.MaximumTitleLength).IsRequired();
        builder.Property(snapshot => snapshot.FirstName).HasMaxLength(OrderAddressSnapshot.MaximumNameLength).IsRequired();
        builder.Property(snapshot => snapshot.LastName).HasMaxLength(OrderAddressSnapshot.MaximumNameLength).IsRequired();
        builder.Property(snapshot => snapshot.PhoneNumber).HasMaxLength(OrderAddressSnapshot.MaximumPhoneNumberLength).IsRequired();
        builder.Property(snapshot => snapshot.City).HasMaxLength(OrderAddressSnapshot.MaximumCityLength).IsRequired();
        builder.Property(snapshot => snapshot.District).HasMaxLength(OrderAddressSnapshot.MaximumDistrictLength).IsRequired();
        builder.Property(snapshot => snapshot.FullAddress).HasMaxLength(OrderAddressSnapshot.MaximumFullAddressLength).IsRequired();
        builder.Property(snapshot => snapshot.PostalCode).HasMaxLength(OrderAddressSnapshot.MaximumPostalCodeLength);
        builder.HasIndex(snapshot => snapshot.SourceAddressId);
        builder.HasIndex(snapshot => snapshot.OrderId).IsUnique();
    }
}
