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
        resources.Should().Contain("ECommerce.Infrastructure.Email.Templates.OrderCreatedEmailTemplate.html");
        resources.Should().Contain("ECommerce.Infrastructure.Email.Templates.PaymentPaidEmailTemplate.html");
        resources.Should().Contain("ECommerce.Infrastructure.Email.Templates.PaymentFailedEmailTemplate.html");
        resources.Should().Contain("ECommerce.Infrastructure.Email.Templates.PaymentReversalCompletedEmailTemplate.html");
        resources.Should().Contain("ECommerce.Infrastructure.Email.Templates.OrderStatusChangedEmailTemplate.html");
        resources.Should().Contain("ECommerce.Infrastructure.Email.Templates.ReturnRequestedEmailTemplate.html");
        resources.Should().Contain("ECommerce.Infrastructure.Email.Templates.ReturnStatusChangedEmailTemplate.html");
        resources.Should().Contain("ECommerce.Infrastructure.Email.Templates.ContactMessageReceivedEmailTemplate.html");
        resources.Should().Contain("ECommerce.Infrastructure.Email.Templates.ContactMessageReplyEmailTemplate.html");
    }

    // Burada müşteri e-postalarının başlığında logo yerine mağaza adı yer tutucusunun bulunduğunu doğruluyorum.
    [Theory]
    [InlineData("PasswordResetEmailTemplate.html")]
    [InlineData("WelcomeEmailTemplate.html")]
    [InlineData("OrderCreatedEmailTemplate.html")]
    [InlineData("PaymentPaidEmailTemplate.html")]
    [InlineData("PaymentFailedEmailTemplate.html")]
    [InlineData("PaymentReversalCompletedEmailTemplate.html")]
    [InlineData("OrderStatusChangedEmailTemplate.html")]
    [InlineData("ReturnRequestedEmailTemplate.html")]
    [InlineData("ReturnStatusChangedEmailTemplate.html")]
    [InlineData("GuestOrderAccessEmailTemplate.html")]
    public void Customer_Email_Templates_Should_Render_Store_Name_Without_Logo(string templateName)
    {
        var assembly = typeof(SmtpEmailSender).Assembly;
        using var stream = assembly.GetManifestResourceStream(
            $"ECommerce.Infrastructure.Email.Templates.{templateName}");
        stream.Should().NotBeNull();
        using var reader = new StreamReader(stream!);
        var template = reader.ReadToEnd();

        template.Should().Contain("{{StoreName}}");
        template.Should().NotContain("{{LogoHtml}}");
        template.Contains("<img", StringComparison.OrdinalIgnoreCase).Should().BeFalse();
    }
}
