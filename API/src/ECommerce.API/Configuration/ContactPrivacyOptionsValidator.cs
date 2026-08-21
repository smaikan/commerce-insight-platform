using ECommerce.Application.Contacts;
using Microsoft.Extensions.Options;

namespace ECommerce.API.Configuration;

public sealed class ContactPrivacyOptionsValidator : IValidateOptions<ContactPrivacyOptions>
{
    private readonly IHostEnvironment _environment;

    // Burada contact privacy ayarlarını go-live blocker değerlendirmesi için ortam bilgisiyle hazırlıyorum.
    public ContactPrivacyOptionsValidator(IHostEnvironment environment) => _environment = environment;

    // Burada privacy notice kaynağını doğrulayıp retention kararı verilmeden production açılışını engelliyorum.
    public ValidateOptionsResult Validate(string? name, ContactPrivacyOptions options)
    {
        var failures = new List<string>();
        if (string.IsNullOrWhiteSpace(options.NoticeVersion) || options.NoticeVersion.Length > 50)
            failures.Add("ContactPrivacy:NoticeVersion must be configured and at most 50 characters.");
        if (_environment.IsProduction() && string.Equals(options.NoticeVersion, "CONFIGURE_BEFORE_GO_LIVE", StringComparison.Ordinal))
            failures.Add("ContactPrivacy:NoticeVersion must be replaced before production go-live.");
        if (options.NoticePublishedAtUtc == default || options.NoticePublishedAtUtc.Offset != TimeSpan.Zero)
            failures.Add("ContactPrivacy:NoticePublishedAtUtc must be configured with the UTC Z offset.");
        if (options.CleanupBatchSize is < 1 or > 1000)
            failures.Add("ContactPrivacy:CleanupBatchSize must be between 1 and 1000.");
        if (_environment.IsProduction() && options.RetentionDays is null)
            failures.Add("ContactPrivacy:RetentionDays is a production go-live blocker until product/legal approves a retention period.");
        if (options.RetentionDays is <= 0)
            failures.Add("ContactPrivacy:RetentionDays must be positive when configured.");
        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}
