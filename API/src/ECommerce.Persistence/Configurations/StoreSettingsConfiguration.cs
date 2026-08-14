using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Persistence.Configurations;

public sealed class StoreSettingsConfiguration : IEntityTypeConfiguration<StoreSettings>
{
    // Burada tek kayıt invariant'ını, alan sınırlarını ve concurrency korumasını veri tabanına yansıtıyorum.
    public void Configure(EntityTypeBuilder<StoreSettings> builder)
    {
        builder.ToTable("StoreSettings", table =>
        {
            table.HasCheckConstraint(
                "CK_StoreSettings_Singleton",
                $"[Id] = '{StoreSettings.SingletonId:D}'");
            table.HasCheckConstraint(
                "CK_StoreSettings_Status",
                "[Status] IN (0, 1, 2)");
            table.HasCheckConstraint(
                "CK_StoreSettings_DefaultProductSort",
                "[DefaultProductSort] IN (0, 1, 2, 3)");
            table.HasCheckConstraint(
                "CK_StoreSettings_LowStockThreshold",
                $"[LowStockThreshold] BETWEEN 1 AND {StoreSettings.MaximumLowStockThreshold}");
        });

        builder.HasKey(settings => settings.Id);
        builder.Property(settings => settings.DisplayName).HasMaxLength(StoreSettings.MaximumDisplayNameLength).IsRequired();
        builder.Property(settings => settings.ShortDescription).HasMaxLength(StoreSettings.MaximumShortDescriptionLength);
        ConfigureUrl(builder.Property(settings => settings.LogoUrl));
        ConfigureUrl(builder.Property(settings => settings.DarkLogoUrl));
        ConfigureUrl(builder.Property(settings => settings.FaviconUrl));
        ConfigureUrl(builder.Property(settings => settings.DefaultShareImageUrl));

        builder.Property(settings => settings.SupportEmail).HasMaxLength(StoreSettings.MaximumEmailLength);
        builder.Property(settings => settings.SupportPhone).HasMaxLength(StoreSettings.MaximumPhoneLength);
        builder.Property(settings => settings.WhatsappNumber).HasMaxLength(StoreSettings.MaximumPhoneLength);
        builder.Property(settings => settings.ContactAddress).HasMaxLength(StoreSettings.MaximumAddressLength);
        builder.Property(settings => settings.WorkingHours).HasMaxLength(StoreSettings.MaximumWorkingHoursLength);
        ConfigureUrl(builder.Property(settings => settings.MapUrl));

        builder.Property(settings => settings.LegalCompanyName).HasMaxLength(StoreSettings.MaximumCompanyNameLength);
        builder.Property(settings => settings.TaxOffice).HasMaxLength(StoreSettings.MaximumShortTextLength);
        builder.Property(settings => settings.TaxNumber).HasMaxLength(StoreSettings.MaximumIdentifierLength);
        builder.Property(settings => settings.NationalIdentityNumber).HasMaxLength(StoreSettings.MaximumIdentifierLength);
        builder.Property(settings => settings.MersisNumber).HasMaxLength(StoreSettings.MaximumIdentifierLength);
        builder.Property(settings => settings.TradeRegistryNumber).HasMaxLength(StoreSettings.MaximumIdentifierLength);
        builder.Property(settings => settings.Country).HasMaxLength(StoreSettings.MaximumShortTextLength);
        builder.Property(settings => settings.City).HasMaxLength(StoreSettings.MaximumShortTextLength);
        builder.Property(settings => settings.District).HasMaxLength(StoreSettings.MaximumShortTextLength);
        builder.Property(settings => settings.AddressLine).HasMaxLength(StoreSettings.MaximumAddressLength);
        builder.Property(settings => settings.PostalCode).HasMaxLength(StoreSettings.MaximumPostalCodeLength);

        builder.Property(settings => settings.DefaultTitle).HasMaxLength(StoreSettings.MaximumSeoTitleLength);
        builder.Property(settings => settings.TitleTemplate).HasMaxLength(StoreSettings.MaximumTitleTemplateLength);
        builder.Property(settings => settings.DefaultDescription).HasMaxLength(StoreSettings.MaximumSeoDescriptionLength);
        ConfigureUrl(builder.Property(settings => settings.DefaultOpenGraphImageUrl));
        ConfigureUrl(builder.Property(settings => settings.FacebookUrl));
        ConfigureUrl(builder.Property(settings => settings.InstagramUrl));
        ConfigureUrl(builder.Property(settings => settings.TiktokUrl));
        ConfigureUrl(builder.Property(settings => settings.YoutubeUrl));
        ConfigureUrl(builder.Property(settings => settings.XUrl));
        ConfigureUrl(builder.Property(settings => settings.PinterestUrl));

        builder.Property(settings => settings.Status).HasConversion<int>().IsRequired();
        builder.Property(settings => settings.StatusMessage).HasMaxLength(StoreSettings.MaximumStatusMessageLength);
        builder.Property(settings => settings.DefaultProductSort).HasConversion<int>().IsRequired();
        builder.Property(settings => settings.ConcurrencyToken).IsConcurrencyToken().IsRequired();
        builder.HasData(StoreSettings.CreateSeed());
    }

    // Burada bütün URL kolonlarına ortak 500 karakter sınırını uyguluyorum.
    private static void ConfigureUrl(PropertyBuilder<string?> property) =>
        property.HasMaxLength(StoreSettings.MaximumUrlLength);
}
