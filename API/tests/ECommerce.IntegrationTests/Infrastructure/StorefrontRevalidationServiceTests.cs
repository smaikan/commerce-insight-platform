using System.Net;
using ECommerce.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ECommerce.IntegrationTests.Infrastructure;

public sealed class StorefrontRevalidationServiceTests
{
    private const string ValidSecret = "storefront-revalidation-test-secret-32-bytes";

    [Fact]
    public void OptionsValidator_Should_Require_A_Strong_Secret_When_Enabled()
    {
        var validator = new StorefrontRevalidationOptionsValidator();
        var options = CreateOptions(secret: "predictable-secret");

        var result = validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(message => message.Contains("between 32 and 512 printable ASCII bytes"));
    }

    [Theory]
    [InlineData("http://storefront-ui:3000/api")]
    [InlineData("http://user:password@storefront-ui:3000")]
    [InlineData("http://storefront-ui:3000?secret=value")]
    public void OptionsValidator_Should_Reject_A_BaseUrl_That_Is_Not_An_Origin(string baseUrl)
    {
        var validator = new StorefrontRevalidationOptionsValidator();

        var result = validator.Validate(null, CreateOptions(baseUrl: baseUrl));

        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(message => message.Contains("without credentials, path, query, or fragment"));
    }

    [Fact]
    public void OptionsValidator_Should_Allow_Missing_Values_When_Feature_Is_Disabled()
    {
        var validator = new StorefrontRevalidationOptionsValidator();

        var result = validator.Validate(null, new StorefrontRevalidationOptions());

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task RevalidateAsync_Should_Send_Secret_Only_In_Header_And_Target_In_Json()
    {
        HttpRequestMessage? capturedRequest = null;
        string? capturedBody = null;
        var handler = new StubHttpMessageHandler(async (request, cancellationToken) =>
        {
            capturedRequest = request;
            capturedBody = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var service = CreateService(handler, CreateOptions());

        await service.RevalidateAsync("products", "/products");

        capturedRequest.Should().NotBeNull();
        capturedRequest!.RequestUri.Should().Be("http://storefront-ui:3000/api/revalidate");
        capturedRequest.RequestUri!.Query.Should().BeEmpty();
        capturedRequest.Headers.GetValues("x-revalidate-secret").Should().ContainSingle().Which.Should().Be(ValidSecret);
        capturedBody.Should().Be("{\"tag\":\"products\",\"path\":\"/products\"}");
    }

    [Fact]
    public async Task RevalidateAsync_Should_Not_Send_A_Request_When_Feature_Is_Disabled()
    {
        var handler = new StubHttpMessageHandler((_, _) =>
            throw new InvalidOperationException("HTTP request should not be sent."));
        var service = CreateService(handler, new StorefrontRevalidationOptions());

        var action = () => service.RevalidateAsync("products", "/products");

        await action.Should().NotThrowAsync();
        handler.RequestCount.Should().Be(0);
    }

    [Fact]
    public async Task RevalidateAsync_Should_Preserve_The_Completed_Mutation_When_Storefront_Fails()
    {
        var handler = new StubHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)));
        var service = CreateService(handler, CreateOptions());

        var action = () => service.RevalidateAsync("products", "/products");

        await action.Should().NotThrowAsync();
        handler.RequestCount.Should().Be(1);
    }

    private static StorefrontRevalidationService CreateService(
        HttpMessageHandler handler,
        StorefrontRevalidationOptions options)
    {
        return new StorefrontRevalidationService(
            new HttpClient(handler),
            Options.Create(options),
            NullLogger<StorefrontRevalidationService>.Instance);
    }

    private static StorefrontRevalidationOptions CreateOptions(
        string baseUrl = "http://storefront-ui:3000",
        string secret = ValidSecret)
    {
        return new StorefrontRevalidationOptions
        {
            Enabled = true,
            BaseUrl = baseUrl,
            Secret = secret
        };
    }

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestCount++;
            return handler(request, cancellationToken);
        }
    }
}
