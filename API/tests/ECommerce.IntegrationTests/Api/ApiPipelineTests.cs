using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.IntegrationTests.Api;

public sealed class ApiPipelineTests
{
    [Fact]
    public async Task Swagger_Document_Should_Be_Served_Through_Real_Http_Pipeline()
    {
        await using var factory = new TestApiFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        using var response = await client.GetAsync("/swagger/v1/swagger.json");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
        var swagger = await response.Content.ReadAsStringAsync();
        swagger.Should().Contain("/api/products");
        swagger.Should().Contain("/api/product-variants/{id}");
        swagger.Should().Contain("/api/auth/login");
        swagger.Should().Contain("/api/users/me");

        using var document = JsonDocument.Parse(swagger);
        var createProductSchema = document.RootElement
            .GetProperty("components")
            .GetProperty("schemas")
            .GetProperty("CreateProductCommand");
        createProductSchema.GetProperty("properties").TryGetProperty("collectionIds", out _).Should().BeTrue();
        createProductSchema.GetProperty("properties").TryGetProperty("variants", out _).Should().BeTrue();
        var requiredProperties = createProductSchema.TryGetProperty("required", out var required)
            ? required.EnumerateArray().Select(property => property.GetString()).ToList()
            : [];
        requiredProperties.Should().NotContain("typeId");
        requiredProperties.Should().NotContain("collectionIds");

        var productDtoProperties = document.RootElement
            .GetProperty("components")
            .GetProperty("schemas")
            .GetProperty("ProductDto")
            .GetProperty("properties");
        productDtoProperties.TryGetProperty("variants", out _).Should().BeTrue();
        productDtoProperties.GetProperty("id").GetProperty("type").GetString().Should().Be("string");

        var variantDtoProperties = document.RootElement
            .GetProperty("components")
            .GetProperty("schemas")
            .GetProperty("ProductVariantDto")
            .GetProperty("properties");
        variantDtoProperties.TryGetProperty("name", out _).Should().BeTrue();
        variantDtoProperties.TryGetProperty("color", out _).Should().BeFalse();
        variantDtoProperties.TryGetProperty("size", out _).Should().BeFalse();
    }

    [Theory]
    [InlineData("/api/products/00000000-0000-0000-0000-000000000001")]
    [InlineData("/api/products/p00001")]
    [InlineData("/api/products/U00001")]
    public async Task Invalid_Product_Public_Id_Should_Return_Bad_Request(string path)
    {
        await using var factory = new TestApiFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var response = await client.GetAsync(path);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Theory]
    [InlineData("/api/products", true)]
    [InlineData("/api/product-types", true)]
    [InlineData("/api/users/me", false)]
    public async Task Protected_Endpoint_Should_Return_Problem_Details_For_Anonymous_Request(
        string path,
        bool usePost)
    {
        await using var factory = new TestApiFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var content = new StringContent("{}", Encoding.UTF8, "application/json");

        using var response = usePost
            ? await client.PostAsync(path, content)
            : await client.GetAsync(path);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        problem.RootElement.GetProperty("code").GetString().Should().Be("authentication_required");
        problem.RootElement.GetProperty("traceId").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Invalid_Jwt_Should_Return_Detailed_Unauthorized_Problem_Details()
    {
        await using var factory = new TestApiFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/users/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-valid-jwt");

        using var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        problem.RootElement.GetProperty("code").GetString().Should().Be("invalid_access_token");
        problem.RootElement.GetProperty("detail").GetString().Should().Contain("Access token validation failed");
        problem.RootElement.GetProperty("traceId").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Validation_Exception_Should_Return_Field_Errors()
    {
        await using var factory = new TestApiFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var content = new StringContent(
            """{"email":"invalid-email","password":""}""",
            Encoding.UTF8,
            "application/json");

        using var response = await client.PostAsync("/api/auth/login", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        problem.RootElement.GetProperty("code").GetString().Should().Be("validation_error");
        problem.RootElement.GetProperty("errors").TryGetProperty("Email", out _).Should().BeTrue();
        problem.RootElement.GetProperty("errors").TryGetProperty("Password", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Unexpected_Exception_Should_Return_Traceable_Problem_Details_In_Development()
    {
        await using var factory = new TestApiFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var response = await client.GetAsync("/api/test-errors/unexpected");

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        problem.RootElement.GetProperty("code").GetString().Should().Be("internal_error");
        problem.RootElement.GetProperty("detail").GetString().Should().Be("Integration test exception.");
        problem.RootElement.GetProperty("exceptionType").GetString().Should().Contain("InvalidOperationException");
        problem.RootElement.GetProperty("traceId").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Unexpected_Exception_Should_Not_Expose_Internal_Details_In_Production()
    {
        await using var factory = new TestApiFactory("Production");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var response = await client.GetAsync("/api/test-errors/unexpected");

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);

        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        problem.RootElement.GetProperty("code").GetString().Should().Be("internal_error");
        problem.RootElement.GetProperty("detail").GetString().Should().NotContain("Integration test exception");
        problem.RootElement.TryGetProperty("exceptionType", out _).Should().BeFalse();
        problem.RootElement.GetProperty("traceId").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Sensitive_Auth_Path_Should_Be_Rate_Limited()
    {
        await using var factory = new TestApiFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var responses = new List<HttpResponseMessage>();

        try
        {
            for (var attempt = 0; attempt < 6; attempt++)
            {
                responses.Add(await client.PostAsync("/api/auth/login", content: null));
            }

            responses.Take(5).Should().OnlyContain(response => response.StatusCode != HttpStatusCode.TooManyRequests);
            responses[5].StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        }
        finally
        {
            foreach (var response in responses)
            {
                response.Dispose();
            }
        }
    }

    private sealed class TestApiFactory : WebApplicationFactory<Program>
    {
        private readonly string _environment;

        public TestApiFactory(string environment = "Development")
        {
            _environment = environment;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment(_environment);
            builder.UseSetting(
                "ConnectionStrings:DefaultConnection",
                "Server=(localdb)\\mssqllocaldb;Database=ECommerceHttpTests;Trusted_Connection=True;");
            builder.UseSetting("Jwt:Issuer", "ECommerce.IntegrationTests");
            builder.UseSetting("Jwt:Audience", "ECommerce.IntegrationTests.Client");
            builder.UseSetting("Jwt:SecretKey", "integration-test-secret-key-at-least-32-bytes");
            builder.ConfigureServices(services =>
                services.AddControllers().AddApplicationPart(typeof(TestExceptionController).Assembly));
        }
    }
}

[ApiController]
[Route("api/test-errors")]
public sealed class TestExceptionController : ControllerBase
{
    [HttpGet("unexpected")]
    public IActionResult ThrowUnexpectedException() =>
        throw new InvalidOperationException("Integration test exception.");
}
