using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Persistence.Configurations;

public sealed class OrderCustomerSnapshotConfiguration : IEntityTypeConfiguration<OrderCustomerSnapshot>
{
    // Burada değişmez sipariş müşteri snapshot tablosunun alan ve tekil ilişki kurallarını tanımlıyorum.
    public void Configure(EntityTypeBuilder<OrderCustomerSnapshot> builder)
    {
        builder.ToTable("OrderCustomerSnapshots");
        builder.HasKey(snapshot => snapshot.Id);
        builder.Property(snapshot => snapshot.FirstName).HasMaxLength(OrderCustomerSnapshot.MaximumNameLength).IsRequired();
        builder.Property(snapshot => snapshot.LastName).HasMaxLength(OrderCustomerSnapshot.MaximumNameLength).IsRequired();
        builder.Property(snapshot => snapshot.Email).HasMaxLength(OrderCustomerSnapshot.MaximumEmailLength).IsRequired();
        builder.Property(snapshot => snapshot.PhoneNumber).HasMaxLength(OrderCustomerSnapshot.MaximumPhoneNumberLength).IsRequired();
        builder.HasIndex(snapshot => snapshot.OrderId).IsUnique();
        builder.HasIndex(snapshot => snapshot.Email);
    }
}
