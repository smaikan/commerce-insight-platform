using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Persistence.Configurations;

public sealed class ReturnRequestConfiguration : IEntityTypeConfiguration<ReturnRequest>
{
    // Burada iade talebi tablosunun parasal alan, ilişki ve iş akışı indekslerini tanımlıyorum.
    public void Configure(EntityTypeBuilder<ReturnRequest> builder)
    {
        builder.ToTable("ReturnRequests", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint("CK_ReturnRequests_UserId_Positive", "[UserId] > 0");
            tableBuilder.HasCheckConstraint("CK_ReturnRequests_RefundTotal_NonNegative", "[RefundTotal] >= 0");
        });

        builder.HasKey(request => request.Id);

        builder.Property(request => request.Type)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(request => request.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(request => request.ReturnNumber)
            .HasMaxLength(ReturnRequest.MaximumReturnNumberLength)
            .IsRequired();

        builder.Property(request => request.RefundTotal)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(request => request.ConcurrencyToken)
            .IsConcurrencyToken()
            .IsRequired();

        builder.Property(request => request.CustomerNote)
            .HasMaxLength(ReturnRequest.MaximumCustomerNoteLength);

        builder.Property(request => request.DecisionNote)
            .HasMaxLength(ReturnRequest.MaximumDecisionNoteLength);

        builder.HasOne(request => request.Order)
            .WithMany()
            .HasForeignKey(request => request.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(request => request.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(request => request.Items)
            .WithOne(item => item.ReturnRequest)
            .HasForeignKey(item => item.ReturnRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(request => request.ReturnNumber).IsUnique();
        builder.HasIndex(request => new { request.OrderId, request.Status });
        builder.HasIndex(request => new { request.UserId, request.CreatedAt });
        builder.HasIndex(request => new { request.Status, request.CreatedAt });
    }
}
