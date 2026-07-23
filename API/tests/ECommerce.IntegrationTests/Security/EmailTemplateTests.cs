using ECommerce.Infrastructure.Email;
using FluentAssertions;

namespace ECommerce.IntegrationTests.Security;

public sealed class EmailTemplateTests
{
    // Burada e-posta worker'ının ihtiyaç duyduğu HTML template'lerinin assembly içine gömüldüğünü doğruluyorum.
    [Fact]
    public void Infrastructure_Assembly_Should_Contain_Email_Templates()
    {
        var resources = typeof(SmtpEmailSender).Assembly.GetManifestResourceNames();

        resources.Should().Contain("ECommerce.Infrastructure.Email.Templates.PasswordResetEmailTemplate.html");
        resources.Should().Contain("ECommerce.Infrastructure.Email.Templates.WelcomeEmailTemplate.html");
    }
}
