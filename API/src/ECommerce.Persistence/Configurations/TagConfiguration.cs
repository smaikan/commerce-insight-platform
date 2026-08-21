using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Persistence.Configurations;

public sealed class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.ToTable("Tags", table => table.UseSqlOutputClause(false));

        builder.HasKey(tag => tag.Id);

        builder.Property(tag => tag.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(tag => tag.Url)
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(tag => tag.Url)
            .IsUnique();

        builder.HasIndex(tag => tag.Name)
            .IsUnique();
    }
}
