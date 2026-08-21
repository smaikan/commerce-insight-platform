using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.IntegrationTests.Api;

public sealed class ApiPipelineTests
{
    // Burada Swagger sözleşmesinin gerçek HTTP hattından ve güncel ürün alanlarıyla sunulduğunu doğruluyorum.
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
        swagger.Should().Contain("/api/cart");
        swagger.Should().Contain("/api/cart/items");
        swagger.Should().Contain("/api/cart/merge-guest");
        swagger.Should().Contain("/api/orders");
        swagger.Should().Contain("/api/orders/mine");
        swagger.Should().Contain("/api/orders/{id}/payments");
        swagger.Should().Contain("/api/orders/import");
        swagger.Should().Contain("/api/orders/import/bulk");
        swagger.Should().Contain("/api/products/performance-metrics");
        swagger.Should().Contain("/api/addresses");
        swagger.Should().Contain("/api/coupons");
        swagger.Should().Contain("/api/stock-movements");
        swagger.Should().Contain("/api/stock-movements/bulk");
        swagger.Should().Contain("/api/product-variants/{id}/stock-movements");
        foreach (var bannerPath in new[]
        {
            "/api/main-banners",
            "/api/alt-banner-1",
            "/api/alt-banner-2",
            "/api/alt-banner-3",
            "/api/alt-banner-4",
            "/api/alt-banner-5"
        })
        {
            swagger.Should().Contain(bannerPath);
        }

        using var document = JsonDocument.Parse(swagger);
        var paths = document.RootElement.GetProperty("paths");
        var memberIyzico = paths.GetProperty("/api/orders/{id}/payments/iyzico/checkout-form")
            .GetProperty("post");
        var guestIyzico = paths.GetProperty("/api/guest-orders/{id}/payments/iyzico/checkout-form")
            .GetProperty("post");
        var callback = paths.GetProperty("/api/payments/iyzico/callback").GetProperty("post");
        var webhook = paths.GetProperty("/api/payments/iyzico/webhook").GetProperty("post");
        memberIyzico.GetProperty("responses").TryGetProperty("201", out _).Should().BeTrue();
        memberIyzico.GetProperty("parameters").EnumerateArray()
            .Single(parameter => parameter.GetProperty("name").GetString() == "Idempotency-Key")
            .GetProperty("required").GetBoolean().Should().BeTrue();
        guestIyzico.GetProperty("security").GetArrayLength().Should().Be(0);
        callback.GetProperty("security").GetArrayLength().Should().Be(0);
        callback.GetProperty("responses").TryGetProperty("303", out _).Should().BeTrue();
        webhook.GetProperty("security").GetArrayLength().Should().Be(0);
        webhook.GetProperty("responses").TryGetProperty("204", out _).Should().BeTrue();
        var checkoutSchema = document.RootElement.GetProperty("components").GetProperty("schemas")
            .GetProperty("CheckoutFormSessionDto");
        checkoutSchema.GetProperty("required").EnumerateArray().Select(item => item.GetString())
            .Should().Contain(["paymentId", "orderId", "provider", "status", "amount", "paymentPageUrl", "expiresAt"]);
        paths.TryGetProperty("/api/storefront-banners", out _).Should().BeFalse();
        foreach (var bannerPath in new[]
        {
            "/api/main-banners",
            "/api/alt-banner-1",
            "/api/alt-banner-2",
            "/api/alt-banner-3",
            "/api/alt-banner-4",
            "/api/alt-banner-5"
        })
        {
            paths.GetProperty(bannerPath).TryGetProperty("get", out _).Should().BeTrue();
            paths.GetProperty(bannerPath).TryGetProperty("put", out _).Should().BeTrue();
            paths.GetProperty($"{bannerPath}/admin").TryGetProperty("get", out _).Should().BeTrue();
        }

        foreach (var path in new[]
        {
            "/api/products/{id}",
            "/api/product-images/{id}",
            "/api/brands/{id}",
            "/api/collections/{id}",
            "/api/product-types/{id}",
            "/api/tags/{id}"
        })
        {
            paths.GetProperty(path)
                .GetProperty("delete")
                .GetProperty("responses")
                .TryGetProperty("204", out _)
                .Should().BeTrue($"{path} DELETE işlemi 204 sözleşmesi yayınlamalıdır");
        }

        var schemas = document.RootElement
            .GetProperty("components")
            .GetProperty("schemas");
        foreach (var schemaName in new[] { "CartItemDto", "OrderItemDto" })
        {
            var variantSchema = schemas.GetProperty(schemaName);
            foreach (var propertyName in new[] { "variantName", "variantValue" })
            {
                var property = variantSchema.GetProperty("properties").GetProperty(propertyName);
                property.GetProperty("type").GetString().Should().Be("string");
                property.GetProperty("nullable").GetBoolean().Should().BeTrue();
                property.GetProperty("maxLength").GetInt32().Should().Be(150);
                AssertOptionalProperty(variantSchema, propertyName);
            }
        }

        var loginSchema = schemas.GetProperty("LoginRequest");
        var loginRequiredProperties = loginSchema
            .GetProperty("required")
            .EnumerateArray()
            .Select(property => property.GetString())
            .ToList();
        loginRequiredProperties.Should().Contain("email");
        loginRequiredProperties.Should().Contain("password");
        loginRequiredProperties.Should().NotContain("deviceName");

        var createProductSchema = schemas.GetProperty("CreateProductCommand");
        createProductSchema.GetProperty("properties").TryGetProperty("mainSku", out _).Should().BeTrue();
        AssertOptionalStringArrayProperty(createProductSchema, "collections");
        createProductSchema.GetProperty("properties").TryGetProperty("variants", out _).Should().BeTrue();
        AssertOptionalStringArrayProperty(createProductSchema, "tags");
        var requiredProperties = createProductSchema.TryGetProperty("required", out var required)
            ? required.EnumerateArray().Select(property => property.GetString()).ToList()
            : [];
        requiredProperties.Should().Contain("mainSku");
        requiredProperties.Should().NotContain("typeId");
        requiredProperties.Should().NotContain("collections");
        requiredProperties.Should().NotContain("tags");

        var updateProductSchema = schemas.GetProperty("UpdateProductRequest");
        AssertOptionalStringArrayProperty(updateProductSchema, "tags");

        var updateRelationsSchema = schemas.GetProperty("UpdateProductRelationsRequest");
        AssertOptionalStringArrayProperty(updateRelationsSchema, "tags");

        var productDtoProperties = schemas
            .GetProperty("ProductDto")
            .GetProperty("properties");
        productDtoProperties.TryGetProperty("mainSku", out _).Should().BeTrue();
        productDtoProperties.TryGetProperty("variants", out _).Should().BeTrue();
        productDtoProperties.GetProperty("id").GetProperty("type").GetString().Should().Be("string");
        var productTagsSchema = productDtoProperties.GetProperty("tags");
        productTagsSchema.GetProperty("type").GetString().Should().Be("array");
        productTagsSchema.GetProperty("items").GetProperty("$ref").GetString()
            .Should().Be("#/components/schemas/TagDto");

        var bulkProductSchema = schemas.GetProperty("BulkCreateProductItem");
        var bulkProductProperties = bulkProductSchema.GetProperty("properties");
        bulkProductProperties.TryGetProperty("collections", out _).Should().BeTrue();
        AssertOptionalStringArrayProperty(bulkProductSchema, "tags");

        var tagDtoProperties = schemas
            .GetProperty("TagDto")
            .GetProperty("properties");
        tagDtoProperties.GetProperty("id").GetProperty("type").GetString().Should().Be("string");
        tagDtoProperties.GetProperty("id").GetProperty("format").GetString().Should().Be("uuid");
        tagDtoProperties.GetProperty("name").GetProperty("type").GetString().Should().Be("string");
        tagDtoProperties.GetProperty("url").GetProperty("type").GetString().Should().Be("string");
        tagDtoProperties.GetProperty("isActive").GetProperty("type").GetString().Should().Be("boolean");

        var brandDtoProperties = schemas
            .GetProperty("BrandDto")
            .GetProperty("properties");
        brandDtoProperties.GetProperty("imageUrl").GetProperty("type").GetString().Should().Be("string");
        brandDtoProperties.GetProperty("imageUrl").GetProperty("nullable").GetBoolean().Should().BeTrue();

        var collectionDtoProperties = schemas
            .GetProperty("CollectionDto")
            .GetProperty("properties");
        collectionDtoProperties.GetProperty("imageUrl").GetProperty("type").GetString().Should().Be("string");
        collectionDtoProperties.GetProperty("imageUrl").GetProperty("nullable").GetBoolean().Should().BeTrue();

        var bannerSectionProperties = schemas
            .GetProperty("BannerSectionDto")
            .GetProperty("properties");
        bannerSectionProperties.GetProperty("name").GetProperty("type").GetString().Should().Be("string");
        bannerSectionProperties.GetProperty("key").GetProperty("type").GetString().Should().Be("string");
        bannerSectionProperties.GetProperty("items").GetProperty("type").GetString().Should().Be("array");
        var bannerItemProperties = schemas
            .GetProperty("BannerItemDto")
            .GetProperty("properties");
        foreach (var property in new[]
        {
            "id", "name", "key", "mediaUrl", "mediaType", "targetUrl", "altText",
            "displayOrder", "isActive", "isMain"
        })
        {
            bannerItemProperties.TryGetProperty(property, out _)
                .Should().BeTrue($"BannerItemDto {property} alanını yayınlamalıdır");
        }

        var variantDtoProperties = schemas
            .GetProperty("ProductVariantDto")
            .GetProperty("properties");
        variantDtoProperties.TryGetProperty("name", out _).Should().BeTrue();
        variantDtoProperties.TryGetProperty("color", out _).Should().BeFalse();
        variantDtoProperties.TryGetProperty("size", out _).Should().BeFalse();

        var adjustStockSchema = schemas.GetProperty("AdjustStockRequest");
        var adjustStockProperties = adjustStockSchema.GetProperty("properties");
        adjustStockProperties.TryGetProperty("quantityDelta", out _).Should().BeTrue();
        adjustStockProperties.TryGetProperty("type", out _).Should().BeTrue();
        adjustStockProperties.TryGetProperty("reason", out _).Should().BeTrue();
        AssertOptionalProperty(adjustStockSchema, "reason");

        var stockMovementProperties = schemas
            .GetProperty("StockMovementDto")
            .GetProperty("properties");
        stockMovementProperties.TryGetProperty("direction", out _).Should().BeTrue();
        stockMovementProperties.TryGetProperty("quantityDelta", out _).Should().BeTrue();
        stockMovementProperties.TryGetProperty("stockBeforeMovement", out _).Should().BeTrue();
        stockMovementProperties.TryGetProperty("stockAfterMovement", out _).Should().BeTrue();

        var orderStatusValues = schemas
            .GetProperty("OrderStatus")
            .GetProperty("enum")
            .EnumerateArray()
            .Select(item => item.GetInt32())
            .ToList();
        orderStatusValues.Should().Contain((int)ECommerce.Domain.Enums.OrderStatus.ReturnRequested);
        orderStatusValues.Should().Contain((int)ECommerce.Domain.Enums.OrderStatus.ReturnApproved);

        var bulkStockMovementSchema = schemas.GetProperty("BulkStockMovementRequest");
        var bulkStockMovementProperties = bulkStockMovementSchema.GetProperty("properties");
        bulkStockMovementProperties.TryGetProperty("productVariantId", out _).Should().BeTrue();
        bulkStockMovementProperties.TryGetProperty("quantityDelta", out _).Should().BeTrue();
        bulkStockMovementProperties.TryGetProperty("type", out _).Should().BeTrue();
        bulkStockMovementProperties.TryGetProperty("reason", out _).Should().BeTrue();
        AssertOptionalProperty(bulkStockMovementSchema, "reason");
    }

    // Burada AdminOnly ürün detayının anonim isteği kimlik doğrulamadan önce 401 ile reddettiğini doğruluyorum.
    [Theory]
    [InlineData("/api/products/00000000-0000-0000-0000-000000000001")]
    [InlineData("/api/products/p00001")]
    [InlineData("/api/products/U00001")]
    public async Task Admin_Product_Detail_Should_Reject_Anonymous_Request_Before_Id_Validation(string path)
    {
        await using var factory = new TestApiFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var response = await client.GetAsync(path);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    // Burada korunan endpointlerin anonim isteklere izlenebilir 401 cevabı verdiğini doğruluyorum.
    [Theory]
    [InlineData("/api/products", true)]
    [InlineData("/api/product-types", true)]
    [InlineData("/api/users/me", false)]
    [InlineData("/api/orders", true)]
    [InlineData("/api/addresses", false)]
    [InlineData("/api/coupons", false)]
    [InlineData("/api/stock-movements", false)]
    [InlineData("/api/stock-movements/bulk", true)]
    [InlineData("/api/product-variants/11111111-1111-1111-1111-111111111111/stock-movements", true)]
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

    // Burada misafir sepeti birleştirme endpointinin anonim isteklerde handler'a ulaşmadan 401 döndürdüğünü doğruluyorum.
    [Fact]
    public async Task Guest_Cart_Merge_Should_Require_Authentication()
    {
        await using var factory = new TestApiFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var response = await client.PostAsync("/api/cart/merge-guest", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        problem.RootElement.GetProperty("code").GetString().Should().Be("authentication_required");
    }

    // Burada geçersiz JWT değerinin ayrıntılı 401 ProblemDetails cevabına dönüştüğünü doğruluyorum.
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

    // Burada validation hatalarının alan bazlı ProblemDetails cevabına dönüştüğünü doğruluyorum.
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

    // Burada geliştirme ortamında beklenmeyen hatanın izlenebilir ayrıntılarla döndüğünü doğruluyorum.
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

    // Burada production ortamında beklenmeyen hata ayrıntılarının istemciye sızmadığını doğruluyorum.
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

    // Burada hassas auth endpointinin istek sınırını aştığında 429 döndürdüğünü doğruluyorum.
    [Theory]
    [InlineData("/api/auth/login")]
    [InlineData("/api/auth/reset-password")]
    public async Task Sensitive_Auth_Path_Should_Be_Rate_Limited(string path)
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
                responses.Add(await client.PostAsync(path, content: null));
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

    // Burada anonim katalog endpointinin IP başına genel istek sınırını uyguladığını doğruluyorum.
    [Fact]
    public async Task Public_Product_List_Should_Not_Be_Rate_Limited()
    {
        await using var factory = new TestApiFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var responses = new List<HttpResponseMessage>();

        try
        {
            for (var attempt = 0; attempt < 121; attempt++)
            {
                responses.Add(await client.GetAsync("/api/products"));
            }

            responses.Should().OnlyContain(response => response.StatusCode != HttpStatusCode.TooManyRequests);
        }
        finally
        {
            foreach (var response in responses)
            {
                response.Dispose();
            }
        }
    }

    // Burada ödeme başlatma endpointinin anonim trafikle brute-force veya kaynak tüketimine açık kalmadığını doğruluyorum.
    [Fact]
    public async Task Payment_Path_Should_Be_Rate_Limited()
    {
        await using var factory = new TestApiFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var responses = new List<HttpResponseMessage>();

        try
        {
            for (var attempt = 0; attempt < 11; attempt++)
            {
                responses.Add(await client.PostAsync($"/api/orders/{Guid.NewGuid()}/payments", new StringContent("{}", Encoding.UTF8, "application/json")));
            }

            responses[^1].StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        }
        finally
        {
            foreach (var response in responses)
            {
                response.Dispose();
            }
        }
    }


    // Burada controller'lardaki her route'un Swagger sözleşmesinde yayınlandığını ve gerçek HTTP hattında doğru anonim/yetki sınırına ulaştığını tek tek doğruluyorum.
    [Fact]
    public async Task Every_Controller_Endpoint_Should_Be_Published_And_Reach_Its_Expected_Authentication_Boundary()
    {
        await using var factory = new TestApiFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var endpoints = DiscoverControllerEndpoints();

        endpoints.Should().NotBeEmpty();

        using var swaggerResponse = await client.GetAsync("/swagger/v1/swagger.json");
        swaggerResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var swagger = JsonDocument.Parse(await swaggerResponse.Content.ReadAsStringAsync());
        var paths = swagger.RootElement.GetProperty("paths");

        foreach (var endpoint in endpoints)
        {
            var openApiPath = ToOpenApiPath(endpoint.RouteTemplate);
            paths.TryGetProperty(openApiPath, out var pathItem).Should().BeTrue(
                $"{endpoint.Method.Method} {endpoint.RouteTemplate} Swagger tarafından yayınlanmalıdır");
            pathItem.TryGetProperty(endpoint.Method.Method.ToLowerInvariant(), out _).Should().BeTrue(
                $"{endpoint.Method.Method} {endpoint.RouteTemplate} HTTP fiili Swagger tarafından yayınlanmalıdır");

            using var request = new HttpRequestMessage(endpoint.Method, MaterializeRoute(endpoint.RouteTemplate));
            if (endpoint.Method != HttpMethod.Get && endpoint.Method != HttpMethod.Delete)
            {
                request.Content = new StringContent("{}", Encoding.UTF8, "application/json");
            }

            using var response = await client.SendAsync(request);
            if (endpoint.AllowsAnonymous)
            {
                var requiresGuestSession = endpoint.RouteTemplate.StartsWith(
                    "/api/guest-orders", StringComparison.Ordinal) &&
                    endpoint.RouteTemplate is not "/api/guest-orders/access-links" and
                    not "/api/guest-orders/access/exchange" ||
                    endpoint.RouteTemplate.StartsWith(
                        "/api/product-engagement/products/", StringComparison.Ordinal) &&
                    endpoint.RouteTemplate.EndsWith("/favorites", StringComparison.Ordinal) &&
                    endpoint.Method != HttpMethod.Get;
                if (!requiresGuestSession)
                {
                    response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized,
                        $"{endpoint.Method.Method} {endpoint.RouteTemplate} AllowAnonymous endpointidir");
                }

                response.StatusCode.Should().NotBe(HttpStatusCode.MethodNotAllowed,
                    $"{endpoint.Method.Method} {endpoint.RouteTemplate} route'a eşlenmelidir");
            }
            else
            {
                response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
                    $"{endpoint.Method.Method} {endpoint.RouteTemplate} handler'a girmeden önce JWT istemelidir");
                response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
            }
        }
    }

    // Burada contact endpointlerinin anonymous/admin güvenlik, header, numeric enum ve ProblemDetails cevap sözleşmelerini doğruluyorum.
    [Fact]
    public async Task OpenApi_Should_Document_Contact_Message_Contracts()
    {
        await using var factory = new TestApiFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var response = await client.GetAsync("/swagger/v1/swagger.json");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;
        var paths = root.GetProperty("paths");
        var submit = paths.GetProperty("/api/contact-messages").GetProperty("post");

        submit.GetProperty("security").GetArrayLength().Should().Be(0);
        var parameters = submit.GetProperty("parameters").EnumerateArray().ToList();
        var idempotency = parameters.Single(parameter => parameter.GetProperty("name").GetString() == "Idempotency-Key");
        idempotency.GetProperty("required").GetBoolean().Should().BeTrue();
        idempotency.GetProperty("schema").GetProperty("maxLength").GetInt32().Should().Be(200);
        var turnstile = parameters.Single(parameter => parameter.GetProperty("name").GetString() == "X-Turnstile-Token");
        turnstile.GetProperty("schema").GetProperty("maxLength").GetInt32().Should().Be(2048);

        var requestReference = submit.GetProperty("requestBody").GetProperty("content").GetProperty("application/json")
            .GetProperty("schema").GetProperty("$ref").GetString()!;
        var schemas = root.GetProperty("components").GetProperty("schemas");
        var requestSchema = schemas.GetProperty(requestReference.Split('/')[^1]);
        var subjectProperty = requestSchema.GetProperty("properties").GetProperty("subject");
        var subjectSchema = subjectProperty.TryGetProperty("$ref", out var subjectReference)
            ? schemas.GetProperty(subjectReference.GetString()!.Split('/')[^1])
            : subjectProperty;
        subjectSchema.GetProperty("enum")
            .EnumerateArray().Select(value => value.GetInt32()).Should().Equal(0, 1, 2, 3, 4, 5);
        requestSchema.GetProperty("properties").GetProperty("message").GetProperty("maxLength").GetInt32().Should().Be(5000);
        foreach (var status in new[] { "202", "400", "409", "413", "428", "429", "503" })
        {
            submit.GetProperty("responses").TryGetProperty(status, out _).Should().BeTrue();
        }

        var adminList = paths.GetProperty("/api/contact-messages").GetProperty("get");
        adminList.TryGetProperty("security", out var adminSecurity).Should().BeFalse();
        adminList.GetProperty("responses").TryGetProperty("400", out var listBadRequest).Should().BeTrue();
        listBadRequest.GetProperty("description").GetString().Should().Contain("validation_error");
        var listParameters = adminList.GetProperty("parameters").EnumerateArray().ToList();
        listParameters.Single(parameter => string.Equals(
                parameter.GetProperty("name").GetString(), "CreatedFromUtc", StringComparison.OrdinalIgnoreCase))
            .GetProperty("description").GetString().Should().Contain("inclusive");
        listParameters.Single(parameter => string.Equals(
                parameter.GetProperty("name").GetString(), "CreatedToUtc", StringComparison.OrdinalIgnoreCase))
            .GetProperty("description").GetString().Should().Contain("inclusive");
        root.GetProperty("security").GetArrayLength().Should().BeGreaterThan(0);
        var reply = paths.GetProperty("/api/contact-messages/{id}/replies").GetProperty("post");
        foreach (var status in new[] { "202", "400", "401", "403", "404", "409" })
        {
            reply.GetProperty("responses").TryGetProperty(status, out _).Should().BeTrue();
        }
        reply.GetProperty("description").GetString().Should().Contain("FirstRespondedAt")
            .And.Contain("WaitingForCustomer");
        reply.GetProperty("responses").GetProperty("400").GetProperty("description")
            .GetString().Should().Contain("validation_error").And.Contain("bad_request").And.Contain("business_rule_violation");
        reply.GetProperty("responses").GetProperty("401").GetProperty("description")
            .GetString().Should().Contain("authentication_required").And.Contain("invalid_access_token");
        reply.GetProperty("responses").GetProperty("403").GetProperty("description")
            .GetString().Should().Contain("forbidden");
        reply.GetProperty("responses").GetProperty("404").GetProperty("description")
            .GetString().Should().Contain("resource_not_found");

        var changeStatus = paths.GetProperty("/api/contact-messages/{id}/status").GetProperty("patch");
        changeStatus.GetProperty("description").GetString().Should().Contain("New (0)");
        changeStatus.GetProperty("description").GetString().Should().Contain("business_rule_violation");
        changeStatus.GetProperty("responses").GetProperty("400").GetProperty("description")
            .GetString().Should().Contain("business_rule_violation");

        schemas.GetProperty("ContactMessageActivityType").GetProperty("description")
            .GetString().Should().Contain("0 Submitted").And.Contain("4 ReplyQueued");
        var activityProperties = schemas.GetProperty("ContactMessageActivityDto").GetProperty("properties");
        activityProperties.GetProperty("actorAdminUserId").GetProperty("description")
            .GetString().Should().Contain("Submitted tipinde null");
        activityProperties.GetProperty("previousValue").GetProperty("description")
            .GetString().Should().Contain("önceki enum adı");
        activityProperties.GetProperty("replyId").GetProperty("description")
            .GetString().Should().Contain("ContactMessageReplyDto.id");

        var detailProperties = schemas.GetProperty("ContactMessageDetailDto").GetProperty("properties");
        schemas.GetProperty("ContactMessageDetailDto").GetProperty("required").EnumerateArray()
            .Select(value => value.GetString()).Should().Contain("message").And.NotContain("phone");
        detailProperties.GetProperty("phone").GetProperty("nullable").GetBoolean().Should().BeTrue();
        detailProperties.GetProperty("activities").GetProperty("description")
            .GetString().Should().Contain("createdAt ASC").And.Contain("id ASC");
        detailProperties.GetProperty("replies").GetProperty("description")
            .GetString().Should().Contain("createdAt ASC").And.Contain("id ASC");
    }

    // Burada controller attribute'larından her HTTP route ve AllowAnonymous kararını çıkarıyorum.
    private static IReadOnlyList<ControllerEndpoint> DiscoverControllerEndpoints()
    {
        var endpoints = new List<ControllerEndpoint>();
        var controllerTypes = typeof(Program).Assembly
            .GetTypes()
            .Where(type => !type.IsAbstract && typeof(Microsoft.AspNetCore.Mvc.ControllerBase).IsAssignableFrom(type));

        foreach (var controllerType in controllerTypes)
        {
            var controllerRoute = controllerType
                .GetCustomAttributes(typeof(Microsoft.AspNetCore.Mvc.RouteAttribute), inherit: true)
                .Cast<Microsoft.AspNetCore.Mvc.RouteAttribute>()
                .SingleOrDefault()?.Template;
            if (string.IsNullOrWhiteSpace(controllerRoute))
            {
                continue;
            }

            var controllerAllowsAnonymous = controllerType
                .GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute), inherit: true)
                .Any();
            foreach (var action in controllerType.GetMethods(
                         System.Reflection.BindingFlags.Instance |
                         System.Reflection.BindingFlags.Public |
                         System.Reflection.BindingFlags.DeclaredOnly))
            {
                var actionAllowsAnonymous = controllerAllowsAnonymous || action
                    .GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute), inherit: true)
                    .Any();
                foreach (var httpAttribute in action
                             .GetCustomAttributes(typeof(Microsoft.AspNetCore.Mvc.Routing.HttpMethodAttribute), inherit: true)
                             .Cast<Microsoft.AspNetCore.Mvc.Routing.HttpMethodAttribute>())
                {
                    var route = string.IsNullOrWhiteSpace(httpAttribute.Template)
                        ? controllerRoute
                        : $"{controllerRoute.TrimEnd('/')}/{httpAttribute.Template.TrimStart('/')}";
                    endpoints.AddRange(httpAttribute.HttpMethods.Select(method => new ControllerEndpoint(
                        new HttpMethod(method),
                        $"/{route}",
                        actionAllowsAnonymous)));
                }
            }
        }

        return endpoints
            .OrderBy(endpoint => endpoint.RouteTemplate, StringComparer.Ordinal)
            .ThenBy(endpoint => endpoint.Method.Method, StringComparer.Ordinal)
            .ToList();
    }

    // Burada ASP.NET route constraint'lerini OpenAPI'nin constraint'siz path biçimine çeviriyorum.
    private static string ToOpenApiPath(string routeTemplate) =>
        System.Text.RegularExpressions.Regex.Replace(routeTemplate, "\\{([^}:]+)(?::[^}]+)?\\}", "{$1}");

    // Burada HTTP hattına ulaşmak için route parametrelerini geçerli sentetik kimliklerle dolduruyorum.
    private static string MaterializeRoute(string routeTemplate) =>
        System.Text.RegularExpressions.Regex.Replace(
            routeTemplate,
            "\\{([^}:]+)(?::[^}]+)?\\}",
            match => match.Groups[1].Value.Equals("productId", StringComparison.OrdinalIgnoreCase)
                ? "P00001"
                : match.Groups[1].Value.Equals("userId", StringComparison.OrdinalIgnoreCase)
                    ? "U00001"
                    : "00000000-0000-0000-0000-000000000001");

    private sealed record ControllerEndpoint(HttpMethod Method, string RouteTemplate, bool AllowsAnonymous);

    private static void AssertOptionalStringArrayProperty(JsonElement schema, string propertyName)
    {
        var property = schema.GetProperty("properties").GetProperty(propertyName);
        property.GetProperty("type").GetString().Should().Be("array");
        property.GetProperty("items").GetProperty("type").GetString().Should().Be("string");

        var requiredProperties = schema.TryGetProperty("required", out var required)
            ? required.EnumerateArray().Select(item => item.GetString()).ToList()
            : [];
        requiredProperties.Should().NotContain(propertyName);
    }

    // Burada bir API alanının Swagger sözleşmesinde zorunlu listeye girmediğini doğruluyorum.
    private static void AssertOptionalProperty(JsonElement schema, string propertyName)
    {
        var requiredProperties = schema.TryGetProperty("required", out var required)
            ? required.EnumerateArray().Select(item => item.GetString()).ToList()
            : [];

        requiredProperties.Should().NotContain(propertyName);
    }

    private sealed class TestApiFactory : WebApplicationFactory<Program>
    {
        private readonly string _environment;

        // Burada API test sunucusunu istenen ortam adıyla hazırlıyorum.
        public TestApiFactory(string environment = "Development")
        {
            _environment = environment;
        }

        // Burada test sunucusunun ortam, bağlantı ve JWT ayarlarını izole biçimde yapılandırıyorum.
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment(_environment);
            var sqlServer = Environment.GetEnvironmentVariable("ECOMMERCE_TEST_SQL_SERVER");
            var sqlPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");
            var connectionString = string.IsNullOrWhiteSpace(sqlServer) || string.IsNullOrWhiteSpace(sqlPassword)
                ? "Server=(localdb)\\mssqllocaldb;Database=ECommerceHttpTests;Trusted_Connection=True;"
                : new SqlConnectionStringBuilder
                {
                    DataSource = sqlServer,
                    InitialCatalog = "ECommerceHttpTests",
                    UserID = "sa",
                    Password = sqlPassword,
                    TrustServerCertificate = true,
                    MultipleActiveResultSets = true
                }.ConnectionString;
            builder.UseSetting(
                "ConnectionStrings:DefaultConnection",
                connectionString);
            builder.UseSetting("Jwt:Issuer", "ECommerce.IntegrationTests");
            builder.UseSetting("Jwt:Audience", "ECommerce.IntegrationTests.Client");
            builder.UseSetting("Jwt:SecretKey", "integration-test-secret-key-at-least-32-bytes");
            builder.UseSetting("Email:PasswordResetUrl", "https://store.test/reset-password");
            builder.UseSetting("Email:Smtp:Password", "integration-test-smtp-secret");
            builder.UseSetting("ContactPrivacy:NoticeVersion", "integration-test-v1");
            builder.UseSetting("ContactPrivacy:RetentionDays", "30");
            builder.UseSetting(
                "DataProtection:KeyRingPath",
                Path.Combine(Path.GetTempPath(), "ECommerce.IntegrationTests.DataProtection"));
            builder.ConfigureServices(services =>
                services.AddControllers().AddApplicationPart(typeof(TestExceptionController).Assembly));
        }
    }
}

[ApiController]
[Route("api/test-errors")]
public sealed class TestExceptionController : ControllerBase
{
    // Burada exception middleware testinin kullanacağı kontrollü beklenmeyen hatayı üretiyorum.
    [HttpGet("unexpected")]
    public IActionResult ThrowUnexpectedException() =>
        throw new InvalidOperationException("Integration test exception.");
}
