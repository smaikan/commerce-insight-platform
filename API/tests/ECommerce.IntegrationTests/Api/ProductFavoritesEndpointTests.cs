using System.Data.Common;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using ECommerce.Application.Carts.Dtos;
using ECommerce.Application.Common.Identifiers;
using ECommerce.Application.Common.Models;
using ECommerce.Application.GuestSessions.Dtos;
using ECommerce.Application.Products.Dtos;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Persistence.Context;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ECommerce.IntegrationTests.Api;

public sealed class ProductFavoritesEndpointTests
{
    private const string GuestCookieName = "ecommerce_guest_cart";
    private const string GuestCsrfHeaderName = "X-Guest-CSRF";
    private const string TrustedOrigin = "https://storefront.test";

    // Burada SQL Server modelinin Products güncellemelerinde trigger uyumlu OUTPUT davranışını kullandığını doğruluyorum.
    [Fact]
    public void Product_Model_Should_Disable_Sql_Output_Clause_For_Trigger_Compatibility()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=MetadataOnly;Trusted_Connection=True")
            .Options;
        using var context = new AppDbContext(options);
        var productType = context.Model.FindEntityType(typeof(Product));

        productType.Should().NotBeNull();
        productType!.IsSqlOutputClauseUsed().Should().BeFalse();
    }

    // Burada yeni anonim ziyaretçiye boş favori listesiyle ortak ve API kapsamlı guest cookie verildiğini doğruluyorum.
    [Fact]
    public async Task Anonymous_Get_Should_Return_Empty_Page_And_Canonical_Guest_Cookie()
    {
        await using var scenario = await FavoriteScenario.CreateAsync();

        using var response = await scenario.Client.GetAsync(
            "/api/product-engagement/favorites?pageNumber=1&pageSize=20");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<PagedResult<ProductDto>>();
        page.Should().NotBeNull();
        page!.Items.Should().BeEmpty();
        page.TotalCount.Should().Be(0);

        var setCookie = GetSetCookieHeaders(response)
            .Single(value => value.StartsWith($"{GuestCookieName}=", StringComparison.Ordinal));
        ExtractGuestSessionId(response).Should().MatchRegex("^[0-9A-F]{64}$");
        setCookie.Should().Contain("path=/api");
        setCookie.Should().Contain("secure");
        setCookie.Should().Contain("httponly");
        setCookie.Should().Contain("samesite=lax");
    }

    // Burada guest ekleme, conflict, listeleme, DTO grafiği, silme ve metrik akışını uçtan uca doğruluyorum.
    [Fact]
    public async Task Guest_Favorite_Flow_Should_Keep_Product_And_Metric_Changes_Atomic()
    {
        await using var scenario = await FavoriteScenario.CreateAsync();
        var sessionId = await scenario.CreateGuestSessionAsync();
        var initial = await scenario.Factory.ReadProductStateAsync(scenario.FirstProductId);

        using var added = await scenario.SendGuestAsync(
            HttpMethod.Post,
            $"/api/product-engagement/products/{scenario.FirstProductPublicId}/favorites",
            sessionId,
            mutation: true);
        added.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var duplicate = await scenario.SendGuestAsync(
            HttpMethod.Post,
            $"/api/product-engagement/products/{scenario.FirstProductPublicId}/favorites",
            sessionId,
            mutation: true);
        duplicate.StatusCode.Should().Be(HttpStatusCode.Conflict);
        using var duplicateProblem = JsonDocument.Parse(await duplicate.Content.ReadAsStringAsync());
        duplicateProblem.RootElement.GetProperty("code").GetString().Should().Be("conflict");

        using var listed = await scenario.SendGuestAsync(
            HttpMethod.Get,
            "/api/product-engagement/favorites?pageNumber=1&pageSize=20",
            sessionId);
        listed.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await listed.Content.ReadFromJsonAsync<PagedResult<ProductDto>>();
        page.Should().NotBeNull();
        page!.Items.Should().ContainSingle();
        AssertCompleteProductGraph(page.Items.Single(), scenario.FirstProductPublicId);

        var afterAdd = await scenario.Factory.ReadProductStateAsync(scenario.FirstProductId);
        afterAdd.FavoriteRows.Should().Be(1);
        afterAdd.FavoriteCount.Should().Be(initial.FavoriteCount + 1);
        afterAdd.PopularityScore.Should().Be(initial.PopularityScore + Product.FavoriteScoreWeight);
        afterAdd.DailyFavoriteCount.Should().Be(initial.DailyFavoriteCount + 1);

        using var removed = await scenario.SendGuestAsync(
            HttpMethod.Delete,
            $"/api/product-engagement/products/{scenario.FirstProductPublicId}/favorites",
            sessionId,
            mutation: true);
        removed.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var afterDeleteList = await scenario.SendGuestAsync(
            HttpMethod.Get,
            "/api/product-engagement/favorites?pageNumber=1&pageSize=20",
            sessionId);
        var afterDeletePage = await afterDeleteList.Content.ReadFromJsonAsync<PagedResult<ProductDto>>();
        afterDeletePage!.Items.Should().BeEmpty();

        var afterDelete = await scenario.Factory.ReadProductStateAsync(scenario.FirstProductId);
        afterDelete.FavoriteRows.Should().Be(0);
        afterDelete.FavoriteCount.Should().Be(initial.FavoriteCount);
        afterDelete.PopularityScore.Should().Be(initial.PopularityScore);
        afterDelete.DailyFavoriteCount.Should().Be(initial.DailyFavoriteCount + 1);
    }

    // Burada iki ayrı guest session'ın birbirinin favorilerini göremediğini doğruluyorum.
    [Fact]
    public async Task Different_Guest_Sessions_Should_Be_Isolated()
    {
        await using var scenario = await FavoriteScenario.CreateAsync();
        using var secondClient = scenario.Factory.CreateFavoriteClient();
        var firstSessionId = await scenario.CreateGuestSessionAsync();
        var secondSessionId = await scenario.CreateGuestSessionAsync(secondClient);

        using var added = await scenario.SendGuestAsync(
            HttpMethod.Post,
            $"/api/product-engagement/products/{scenario.FirstProductPublicId}/favorites",
            firstSessionId,
            mutation: true);
        added.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var firstList = await scenario.SendGuestAsync(
            HttpMethod.Get,
            "/api/product-engagement/favorites",
            firstSessionId);
        var firstPage = await firstList.Content.ReadFromJsonAsync<PagedResult<ProductDto>>();
        firstPage!.Items.Should().ContainSingle(item => item.Id == scenario.FirstProductPublicId);

        using var secondList = await scenario.SendGuestAsync(
            HttpMethod.Get,
            "/api/product-engagement/favorites",
            secondSessionId,
            client: secondClient);
        var secondPage = await secondList.Content.ReadFromJsonAsync<PagedResult<ProductDto>>();
        secondPage!.Items.Should().BeEmpty();
    }

    // Burada geçerli JWT sahibinin aynı istekteki guest cookie'den öncelikli olduğunu doğruluyorum.
    [Fact]
    public async Task Authenticated_User_Should_Take_Precedence_Over_Guest_Session()
    {
        await using var scenario = await FavoriteScenario.CreateAsync();
        var sessionId = await scenario.CreateGuestSessionAsync();
        using var guestAdd = await scenario.SendGuestAsync(
            HttpMethod.Post,
            $"/api/product-engagement/products/{scenario.FirstProductPublicId}/favorites",
            sessionId,
            mutation: true);
        guestAdd.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var userListBefore = await scenario.SendUserAsync(
            HttpMethod.Get,
            "/api/product-engagement/favorites",
            scenario.FirstUserPublicId,
            sessionId);
        var emptyUserPage = await userListBefore.Content.ReadFromJsonAsync<PagedResult<ProductDto>>();
        emptyUserPage!.Items.Should().BeEmpty();

        using var userAdd = await scenario.SendUserAsync(
            HttpMethod.Post,
            $"/api/product-engagement/products/{scenario.FirstProductPublicId}/favorites",
            scenario.FirstUserPublicId,
            sessionId);
        userAdd.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var userListAfter = await scenario.SendUserAsync(
            HttpMethod.Get,
            "/api/product-engagement/favorites",
            scenario.FirstUserPublicId,
            sessionId);
        var userPage = await userListAfter.Content.ReadFromJsonAsync<PagedResult<ProductDto>>();
        userPage!.Items.Should().ContainSingle(item => item.Id == scenario.FirstProductPublicId);

        using var otherUserList = await scenario.SendUserAsync(
            HttpMethod.Get,
            "/api/product-engagement/favorites",
            scenario.SecondUserPublicId,
            sessionId);
        var otherUserPage = await otherUserList.Content.ReadFromJsonAsync<PagedResult<ProductDto>>();
        otherUserPage!.Items.Should().BeEmpty();

        var ownership = await scenario.Factory.ReadFavoriteOwnershipAsync(
            scenario.FirstProductId,
            scenario.FirstUserId,
            sessionId);
        ownership.UserRows.Should().Be(1);
        ownership.GuestRows.Should().Be(1);
    }

    // Burada hatalı Bearer gönderildiğinde isteğin sessizce guest sahipliğine düşmeyip 401 aldığını doğruluyorum.
    [Fact]
    public async Task Invalid_Bearer_Should_Not_Fall_Back_To_Guest_Session()
    {
        await using var scenario = await FavoriteScenario.CreateAsync();
        var sessionId = await scenario.CreateGuestSessionAsync();
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/product-engagement/favorites");
        request.Headers.Add("Cookie", $"{GuestCookieName}={sessionId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");

        using var response = await scenario.Client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        problem.RootElement.GetProperty("status").GetInt32().Should().Be(401);
    }

    // Burada guest favori liste sorgusunun ürün sayısıyla artmayan sabit split query sayısını koruduğunu doğruluyorum.
    [Fact]
    public async Task Guest_Get_Should_Use_Fixed_Split_Query_Count()
    {
        await using var scenario = await FavoriteScenario.CreateAsync();
        var sessionId = await scenario.CreateGuestSessionAsync();
        await scenario.Factory.AddDirectGuestFavoriteAsync(scenario.FirstProductId, sessionId);
        await scenario.Factory.AddDirectGuestFavoriteAsync(scenario.SecondProductId, sessionId);
        scenario.Factory.CommandCounter.Reset();

        using var response = await scenario.SendGuestAsync(
            HttpMethod.Get,
            "/api/product-engagement/favorites?pageNumber=1&pageSize=20",
            sessionId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<PagedResult<ProductDto>>();
        page!.Items.Should().HaveCount(2);
        page.Items.Select(item => item.Title).Should().BeInAscendingOrder();
        scenario.Factory.CommandCounter.ReaderCommandCount.Should().Be(6);
    }

    // Burada üyenin sepeti ve favorileri boşken guest verilerinin kullanıcıya devredildiğini doğruluyorum.
    [Fact]
    public async Task Claim_Should_Transfer_Guest_Data_When_User_State_Is_Empty()
    {
        await using var scenario = await FavoriteScenario.CreateAsync();
        var sessionId = await scenario.CreateGuestSessionAsync();
        await scenario.Factory.AddEmptyUserCartAsync(scenario.FirstUserId);

        using var guestFavorite = await scenario.SendGuestAsync(
            HttpMethod.Post,
            $"/api/product-engagement/products/{scenario.FirstProductPublicId}/favorites",
            sessionId,
            mutation: true);
        guestFavorite.StatusCode.Should().Be(HttpStatusCode.NoContent);
        using var guestCart = await scenario.SendJsonGuestAsync(
            HttpMethod.Post,
            "/api/cart/items",
            new { ProductVariantId = scenario.FirstVariantId, Quantity = 2 },
            sessionId);
        guestCart.StatusCode.Should().Be(HttpStatusCode.OK);
        var beforeClaim = await scenario.Factory.ReadProductStateAsync(scenario.FirstProductId);

        using var claim = await scenario.SendUserAsync(
            HttpMethod.Post,
            "/api/guest-session/claim",
            scenario.FirstUserPublicId,
            sessionId);

        claim.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await claim.Content.ReadFromJsonAsync<GuestSessionClaimDto>();
        result.Should().NotBeNull();
        result!.FavoriteCount.Should().Be(1);
        result.Cart.Items.Should().ContainSingle(item =>
            item.ProductId == scenario.FirstProductPublicId && item.Quantity == 2);
        AssertGuestCookieDeleted(claim);

        var ownerState = await scenario.Factory.ReadOwnerStateAsync(scenario.FirstUserId, sessionId);
        ownerState.UserCartRows.Should().Be(1);
        ownerState.GuestCartRows.Should().Be(0);
        ownerState.UserFavoriteRows.Should().Be(1);
        ownerState.GuestFavoriteRows.Should().Be(0);
        var afterClaim = await scenario.Factory.ReadProductStateAsync(scenario.FirstProductId);
        afterClaim.Should().Be(beforeClaim);
    }

    // Burada üyenin dolu sepeti ve favorisi varsa bunların korunduğunu ve guest verilerinin temizlendiğini doğruluyorum.
    [Fact]
    public async Task Claim_Should_Keep_NonEmpty_User_State_And_Discard_Guest_State()
    {
        await using var scenario = await FavoriteScenario.CreateAsync();
        var sessionId = await scenario.CreateGuestSessionAsync();

        using var guestFavorite = await scenario.SendGuestAsync(
            HttpMethod.Post,
            $"/api/product-engagement/products/{scenario.FirstProductPublicId}/favorites",
            sessionId,
            mutation: true);
        guestFavorite.StatusCode.Should().Be(HttpStatusCode.NoContent);
        using var guestCart = await scenario.SendJsonGuestAsync(
            HttpMethod.Post,
            "/api/cart/items",
            new { ProductVariantId = scenario.FirstVariantId, Quantity = 1 },
            sessionId);
        guestCart.StatusCode.Should().Be(HttpStatusCode.OK);

        using var userFavorite = await scenario.SendUserAsync(
            HttpMethod.Post,
            $"/api/product-engagement/products/{scenario.SecondProductPublicId}/favorites",
            scenario.FirstUserPublicId);
        userFavorite.StatusCode.Should().Be(HttpStatusCode.NoContent);
        using var userCart = await scenario.SendJsonUserAsync(
            HttpMethod.Post,
            "/api/cart/items",
            new { ProductVariantId = scenario.SecondVariantId, Quantity = 3 },
            scenario.FirstUserPublicId);
        userCart.StatusCode.Should().Be(HttpStatusCode.OK);

        var guestProductBeforeClaim = await scenario.Factory.ReadProductStateAsync(scenario.FirstProductId);
        var userProductBeforeClaim = await scenario.Factory.ReadProductStateAsync(scenario.SecondProductId);

        using var claim = await scenario.SendUserAsync(
            HttpMethod.Post,
            "/api/guest-session/claim",
            scenario.FirstUserPublicId,
            sessionId);

        claim.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await claim.Content.ReadFromJsonAsync<GuestSessionClaimDto>();
        result!.FavoriteCount.Should().Be(1);
        result.Cart.Items.Should().ContainSingle(item =>
            item.ProductId == scenario.SecondProductPublicId && item.Quantity == 3);
        AssertGuestCookieDeleted(claim);

        var ownerState = await scenario.Factory.ReadOwnerStateAsync(scenario.FirstUserId, sessionId);
        ownerState.UserCartRows.Should().Be(1);
        ownerState.GuestCartRows.Should().Be(0);
        ownerState.UserFavoriteRows.Should().Be(1);
        ownerState.GuestFavoriteRows.Should().Be(0);

        var guestProductAfterClaim = await scenario.Factory.ReadProductStateAsync(scenario.FirstProductId);
        guestProductAfterClaim.FavoriteCount.Should().Be(guestProductBeforeClaim.FavoriteCount - 1);
        guestProductAfterClaim.PopularityScore.Should().Be(
            guestProductBeforeClaim.PopularityScore - Product.FavoriteScoreWeight);
        guestProductAfterClaim.DailyFavoriteCount.Should().Be(guestProductBeforeClaim.DailyFavoriteCount);

        var userProductAfterClaim = await scenario.Factory.ReadProductStateAsync(scenario.SecondProductId);
        userProductAfterClaim.Should().Be(userProductBeforeClaim);
    }

    // Burada eski cart merge endpointinin ortak claim servisiyle guest favorilerini de kullanıcıya aktardığını doğruluyorum.
    [Fact]
    public async Task Legacy_Cart_Merge_Should_Also_Claim_Guest_Favorites()
    {
        await using var scenario = await FavoriteScenario.CreateAsync();
        var sessionId = await scenario.CreateGuestSessionAsync();
        using var favorite = await scenario.SendGuestAsync(
            HttpMethod.Post,
            $"/api/product-engagement/products/{scenario.FirstProductPublicId}/favorites",
            sessionId,
            mutation: true);
        favorite.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var merge = await scenario.SendUserAsync(
            HttpMethod.Post,
            "/api/cart/merge-guest",
            scenario.FirstUserPublicId,
            sessionId);

        merge.StatusCode.Should().Be(HttpStatusCode.OK);
        var cart = await merge.Content.ReadFromJsonAsync<CartDto>();
        cart!.Items.Should().BeEmpty();
        AssertGuestCookieDeleted(merge);

        using var userList = await scenario.SendUserAsync(
            HttpMethod.Get,
            "/api/product-engagement/favorites",
            scenario.FirstUserPublicId);
        var page = await userList.Content.ReadFromJsonAsync<PagedResult<ProductDto>>();
        page!.Items.Should().ContainSingle(item => item.Id == scenario.FirstProductPublicId);
    }

    // Burada OpenAPI'nin anonymous güvenlik ve favori/claim cevap sözleşmelerini doğru ürettiğini doğruluyorum.
    [Fact]
    public async Task OpenApi_Should_Describe_Guest_Favorite_And_Claim_Contracts()
    {
        await using var scenario = await FavoriteScenario.CreateAsync();

        using var response = await scenario.Client.GetAsync("/swagger/v1/swagger.json");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var paths = document.RootElement.GetProperty("paths");
        var get = paths.GetProperty("/api/product-engagement/favorites").GetProperty("get");
        var favoritePath = paths.GetProperty(
            "/api/product-engagement/products/{productId}/favorites");
        var post = favoritePath.GetProperty("post");
        var delete = favoritePath.GetProperty("delete");
        var claim = paths.GetProperty("/api/guest-session/claim").GetProperty("post");

        AssertAnonymousOperation(get);
        AssertAnonymousOperation(post);
        AssertAnonymousOperation(delete);
        AssertResponseCodes(get, "200", "400", "401");
        AssertResponseCodes(post, "204", "400", "401", "403", "404", "409");
        AssertResponseCodes(delete, "204", "400", "401", "403", "404");
        get.GetProperty("responses").GetProperty("200").GetProperty("content")
            .GetProperty("application/json").GetProperty("schema").ValueKind
            .Should().Be(JsonValueKind.Object);
        AssertResponseCodes(claim, "200", "400", "401", "409");
        claim.TryGetProperty("security", out var claimSecurity).Should().BeFalse(
            "claim endpointi global Bearer güvenlik gereksinimini kullanmalıdır");
    }

    // Burada favori DTO'sunun ProductDto için gerekli bütün ilişki grafiğini taşıdığını doğruluyorum.
    private static void AssertCompleteProductGraph(ProductDto product, string expectedProductId)
    {
        product.Id.Should().Be(expectedProductId);
        product.TypeName.Should().Be("Favori Türü");
        product.BrandName.Should().Be("Favori Markası");
        product.TaxRateName.Should().Be("Standart KDV");
        product.Variants.Should().ContainSingle(variant => variant.Sku == "FAVORI-V1");
        product.Images.Should().ContainSingle(image => image.ImageUrl == "https://cdn.test/favorite.jpg");
        product.Collections.Should().ContainSingle(collection => collection.Name == "Favori Koleksiyonu");
        product.Tags.Should().ContainSingle(tag => tag.Name == "Favori Etiketi");
    }

    // Burada anonymous OpenAPI operasyonunun global Bearer gereksinimini boş security dizisiyle ezdiğini doğruluyorum.
    private static void AssertAnonymousOperation(JsonElement operation)
    {
        operation.GetProperty("security").GetArrayLength().Should().Be(0);
    }

    // Burada OpenAPI operasyonunda beklenen bütün durum kodlarının bulunduğunu doğruluyorum.
    private static void AssertResponseCodes(JsonElement operation, params string[] expectedCodes)
    {
        var responses = operation.GetProperty("responses");
        foreach (var expectedCode in expectedCodes)
        {
            responses.TryGetProperty(expectedCode, out _).Should().BeTrue(
                $"{expectedCode} cevabı sözleşmede bulunmalıdır");
        }
    }

    // Burada claim cevabının ortak ve eski cart path'leri için guest cookie silme başlıklarını taşıdığını doğruluyorum.
    private static void AssertGuestCookieDeleted(HttpResponseMessage response)
    {
        var deleteHeaders = GetSetCookieHeaders(response)
            .Where(value => value.StartsWith($"{GuestCookieName}=", StringComparison.Ordinal))
            .ToList();
        deleteHeaders.Should().Contain(value => value.Contains("path=/api", StringComparison.OrdinalIgnoreCase));
        deleteHeaders.Should().Contain(value => value.Contains("path=/api/cart", StringComparison.OrdinalIgnoreCase));
        deleteHeaders.Should().OnlyContain(value =>
            value.StartsWith($"{GuestCookieName}=;", StringComparison.Ordinal) ||
            value.StartsWith($"{GuestCookieName}=; ", StringComparison.Ordinal));
    }

    // Burada Set-Cookie başlıklarından canonical guest session değerini güvenli biçimde ayıklıyorum.
    private static string ExtractGuestSessionId(HttpResponseMessage response)
    {
        var cookie = GetSetCookieHeaders(response)
            .Single(value => value.StartsWith($"{GuestCookieName}=", StringComparison.Ordinal));
        return cookie[(GuestCookieName.Length + 1)..].Split(';', 2)[0];
    }

    // Burada HTTP cevabındaki tüm Set-Cookie başlıklarını tek koleksiyonda okuyorum.
    private static IReadOnlyList<string> GetSetCookieHeaders(HttpResponseMessage response)
    {
        response.Headers.TryGetValues("Set-Cookie", out var values).Should().BeTrue();
        return values!.ToList();
    }

    private sealed class FavoriteScenario : IAsyncDisposable
    {
        // Burada izole favori senaryosunun HTTP ve kalıcı kimlik bilgilerini hazırlıyorum.
        private FavoriteScenario(
            FavoriteApiFactory factory,
            HttpClient client,
            FavoriteSeed seed)
        {
            Factory = factory;
            Client = client;
            FirstProductId = seed.FirstProductId;
            SecondProductId = seed.SecondProductId;
            FirstVariantId = seed.FirstVariantId;
            SecondVariantId = seed.SecondVariantId;
            FirstUserId = seed.FirstUserId;
            FirstProductPublicId = PublicIdCodec.EncodeProductId(seed.FirstProductId);
            SecondProductPublicId = PublicIdCodec.EncodeProductId(seed.SecondProductId);
            FirstUserPublicId = PublicIdCodec.EncodeUserId(seed.FirstUserId);
            SecondUserPublicId = PublicIdCodec.EncodeUserId(seed.SecondUserId);
        }

        public FavoriteApiFactory Factory { get; }
        public HttpClient Client { get; }
        public long FirstProductId { get; }
        public long SecondProductId { get; }
        public Guid FirstVariantId { get; }
        public Guid SecondVariantId { get; }
        public long FirstUserId { get; }
        public string FirstProductPublicId { get; }
        public string SecondProductPublicId { get; }
        public string FirstUserPublicId { get; }
        public string SecondUserPublicId { get; }

        // Burada izole API hostunu başlatıp kullanıcı, ürün ve DTO ilişkilerini hazırlıyorum.
        public static async Task<FavoriteScenario> CreateAsync()
        {
            var factory = new FavoriteApiFactory();
            var client = factory.CreateFavoriteClient();
            var seed = await factory.InitializeAndSeedAsync();
            return new FavoriteScenario(factory, client, seed);
        }

        // Burada anonim GET üzerinden API'nin ürettiği ortak guest session değerini alıyorum.
        public async Task<string> CreateGuestSessionAsync(HttpClient? client = null)
        {
            using var response = await (client ?? Client).GetAsync(
                "/api/product-engagement/favorites?pageNumber=1&pageSize=20");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            return ExtractGuestSessionId(response);
        }

        // Burada cookie ve gerekiyorsa Origin/CSRF başlıklarıyla guest isteği gönderiyorum.
        public Task<HttpResponseMessage> SendGuestAsync(
            HttpMethod method,
            string path,
            string sessionId,
            bool mutation = false,
            HttpClient? client = null)
        {
            var request = CreateGuestRequest(method, path, sessionId, mutation);
            return (client ?? Client).SendAsync(request);
        }

        // Burada JSON gövdeli guest isteğini ortak session başlıklarıyla gönderiyorum.
        public Task<HttpResponseMessage> SendJsonGuestAsync<TBody>(
            HttpMethod method,
            string path,
            TBody body,
            string sessionId)
        {
            var request = CreateGuestRequest(method, path, sessionId, mutation: false);
            request.Content = JsonContent.Create(body);
            return Client.SendAsync(request);
        }

        // Burada seçilen test kullanıcısıyla ve isteğe bağlı guest cookie ile yetkili HTTP isteği gönderiyorum.
        public Task<HttpResponseMessage> SendUserAsync(
            HttpMethod method,
            string path,
            string userPublicId,
            string? guestSessionId = null)
        {
            var request = new HttpRequestMessage(method, path);
            request.Headers.Add(FavoriteAuthenticationHandler.UserHeaderName, userPublicId);
            if (guestSessionId is not null)
            {
                request.Headers.Add("Cookie", $"{GuestCookieName}={guestSessionId}");
            }

            return Client.SendAsync(request);
        }

        // Burada JSON gövdeli yetkili isteği test kullanıcısının claim'iyle gönderiyorum.
        public Task<HttpResponseMessage> SendJsonUserAsync<TBody>(
            HttpMethod method,
            string path,
            TBody body,
            string userPublicId)
        {
            var request = new HttpRequestMessage(method, path)
            {
                Content = JsonContent.Create(body)
            };
            request.Headers.Add(FavoriteAuthenticationHandler.UserHeaderName, userPublicId);
            return Client.SendAsync(request);
        }

        // Burada guest HTTP isteğine canonical cookie ve mutation güvenlik başlıklarını ekliyorum.
        private static HttpRequestMessage CreateGuestRequest(
            HttpMethod method,
            string path,
            string sessionId,
            bool mutation)
        {
            var request = new HttpRequestMessage(method, path);
            request.Headers.Add("Cookie", $"{GuestCookieName}={sessionId}");
            if (mutation)
            {
                request.Headers.Add("Origin", TrustedOrigin);
                request.Headers.Add(GuestCsrfHeaderName, sessionId);
            }

            return request;
        }

        // Burada HTTP client ve test hostu yaşam döngüsünü birlikte kapatıyorum.
        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await Factory.DisposeAsync();
        }
    }

    private sealed class FavoriteApiFactory : WebApplicationFactory<Program>
    {
        private readonly SqliteConnection _connection = new("Data Source=:memory:;Foreign Keys=True");
        public CommandCaptureInterceptor CommandCounter { get; } = new();

        // Burada test boyunca ortak ilişkisel SQLite bağlantısını açık tutuyorum.
        public FavoriteApiFactory()
        {
            _connection.Open();
        }

        // Burada API hattını SQLite, trusted Origin ve kullanıcı seçebilen test authentication şemasıyla çalıştırıyorum.
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("ConnectionStrings:DefaultConnection", "unused-by-test-override");
            builder.UseSetting("Jwt:Issuer", "ECommerce.FavoriteIntegrationTests");
            builder.UseSetting("Jwt:Audience", "ECommerce.FavoriteIntegrationTests.Client");
            builder.UseSetting("Jwt:SecretKey", "favorite-integration-test-secret-key-at-least-32-bytes");
            builder.UseSetting("GuestProtection:TrustedOrigins", TrustedOrigin);
            builder.UseSetting(
                "DataProtection:KeyRingPath",
                Path.Combine(Path.GetTempPath(), "ECommerce.FavoriteTests.Keys"));
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<AppDbContext>();
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
                foreach (var hostedService in services
                    .Where(descriptor => descriptor.ServiceType == typeof(IHostedService))
                    .ToList())
                {
                    services.Remove(hostedService);
                }

                services.AddDbContext<AppDbContext>(options =>
                    options.UseSqlite(_connection).AddInterceptors(CommandCounter));
            });
            builder.ConfigureTestServices(services =>
            {
                services.AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme = FavoriteAuthenticationHandler.SchemeName;
                        options.DefaultChallengeScheme = FavoriteAuthenticationHandler.SchemeName;
                        options.DefaultForbidScheme = FavoriteAuthenticationHandler.SchemeName;
                    })
                    .AddScheme<AuthenticationSchemeOptions, FavoriteAuthenticationHandler>(
                        FavoriteAuthenticationHandler.SchemeName,
                        _ => { });
            });
        }

        // Burada cookie'leri testte açıkça yönetecek HTTPS tabanlı HTTP istemcisini oluşturuyorum.
        public HttpClient CreateFavoriteClient()
        {
            return CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                BaseAddress = new Uri("https://localhost"),
                HandleCookies = false
            });
        }

        // Burada iki kullanıcı ile iki aktif ürün ve ilk ürünün bütün ProductDto ilişkilerini hazırlıyorum.
        public async Task<FavoriteSeed> InitializeAndSeedAsync()
        {
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await context.Database.EnsureCreatedAsync();
            var firstUser = new User("favorite-one@test.local", "hash", "Favori", "Bir");
            var secondUser = new User("favorite-two@test.local", "hash", "Favori", "İki");
            var type = new ProductType("Favori Türü");
            var brand = new Brand("Favori Markası", "favori-markasi");
            var taxRate = new TaxRate("Standart KDV", 20m);
            var collection = new Collection("Favori Koleksiyonu", "favori-koleksiyonu");
            var tag = new Tag("Favori Etiketi", "favori-etiketi");
            context.AddRange(firstUser, secondUser, type, brand, taxRate, collection, tag);
            await context.SaveChangesAsync();

            var firstProduct = new Product(
                "Favori Ürünü",
                "favori-urunu",
                "FAVORI-MAIN",
                type.Id,
                brand.Id,
                status: ProductStatus.Active,
                taxRateId: taxRate.Id,
                hasVariants: true);
            var firstVariant = new ProductVariant(
                firstProduct,
                "Renk",
                "FAVORI-V1",
                240m,
                8,
                value: "Mavi");
            firstProduct.Variants.Add(firstVariant);
            firstProduct.Images.Add(new ProductImage(
                firstProduct,
                "https://cdn.test/favorite.jpg",
                isMain: true,
                altText: "Favori ürünü"));
            firstProduct.ProductCollections.Add(new ProductCollection(firstProduct, collection.Id));
            firstProduct.ProductTags.Add(new ProductTag(firstProduct, tag.Id));

            var secondProduct = new Product(
                "İkinci Favori Ürünü",
                "ikinci-favori-urunu",
                "FAVORI-SECOND",
                type.Id,
                status: ProductStatus.Active,
                hasVariants: true);
            var secondVariant = new ProductVariant(
                secondProduct,
                "Boyut",
                "FAVORI-V2",
                120m,
                10,
                value: "Standart");
            secondProduct.Variants.Add(secondVariant);
            context.Products.AddRange(firstProduct, secondProduct);
            await context.SaveChangesAsync();
            CommandCounter.Reset();

            return new FavoriteSeed(
                firstProduct.Id,
                secondProduct.Id,
                firstVariant.Id,
                secondVariant.Id,
                firstUser.Id,
                secondUser.Id);
        }

        // Burada favori sayaçları ile günlük metriğin kalıcı durumunu takip olmadan okuyorum.
        public async Task<ProductFavoriteState> ReadProductStateAsync(long productId)
        {
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var product = await context.Products.AsNoTracking().SingleAsync(item => item.Id == productId);
            return new ProductFavoriteState(
                product.FavoriteCount,
                product.PopularityScore,
                await context.FavoriteProducts.AsNoTracking().CountAsync(item => item.ProductId == productId),
                await context.ProductDailyMetrics.AsNoTracking()
                    .Where(item => item.ProductId == productId)
                    .Select(item => (long?)item.FavoriteCount)
                    .SingleOrDefaultAsync() ?? 0);
        }

        // Burada belirli ürünün user ve guest favori sahiplik satırlarını ayrı ayrı sayıyorum.
        public async Task<FavoriteOwnershipState> ReadFavoriteOwnershipAsync(
            long productId,
            long userId,
            string sessionId)
        {
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            return new FavoriteOwnershipState(
                await context.FavoriteProducts.AsNoTracking().CountAsync(item =>
                    item.ProductId == productId && item.UserId == userId),
                await context.FavoriteProducts.AsNoTracking().CountAsync(item =>
                    item.ProductId == productId && item.UserId == null && item.SessionId == sessionId));
        }

        // Burada claim sonrasında user ve guest cart/favorite kayıtlarının hangi tarafta kaldığını sayıyorum.
        public async Task<OwnerState> ReadOwnerStateAsync(long userId, string sessionId)
        {
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            return new OwnerState(
                await context.Carts.AsNoTracking().CountAsync(cart => cart.UserId == userId),
                await context.Carts.AsNoTracking().CountAsync(cart =>
                    cart.UserId == null && cart.SessionId == sessionId),
                await context.FavoriteProducts.AsNoTracking().CountAsync(item => item.UserId == userId),
                await context.FavoriteProducts.AsNoTracking().CountAsync(item =>
                    item.UserId == null && item.SessionId == sessionId));
        }

        // Burada boş sepet kaydı bulunan kullanıcı dalının claim davranışını sınamak için aggregate oluşturuyorum.
        public async Task AddEmptyUserCartAsync(long userId)
        {
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            context.Carts.Add(Cart.CreateForUser(userId));
            await context.SaveChangesAsync();
        }

        // Burada N+1 testine guest session sahibi favori ilişkisini doğrudan ekliyorum.
        public async Task AddDirectGuestFavoriteAsync(long productId, string sessionId)
        {
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            context.FavoriteProducts.Add(new FavoriteProduct(productId, sessionId));
            await context.SaveChangesAsync();
        }

        // Burada açık SQLite bağlantısını test hostuyla birlikte kapatıyorum.
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                _connection.Dispose();
            }
        }
    }

    private sealed class FavoriteAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string SchemeName = "FavoriteEndpointTest";
        public const string UserHeaderName = "X-Test-User";

        // Burada test authentication handler bağımlılıklarını framework tabanına iletiyorum.
        public FavoriteAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        // Burada header içindeki public kullanıcı kimliğini doğrulanmış test claim'ine dönüştürüyorum.
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var userPublicId = Request.Headers[UserHeaderName].ToString();
            if (string.IsNullOrWhiteSpace(userPublicId))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var identity = new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, userPublicId)],
                SchemeName);
            var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }

    private sealed class CommandCaptureInterceptor : DbCommandInterceptor
    {
        public int ReaderCommandCount { get; private set; }

        // Burada favori liste isteğinden önce SQL reader sayacını sıfırlıyorum.
        public void Reset() => ReaderCommandCount = 0;

        // Burada N+1 kontrolü için çalıştırılan reader komutlarını sayıyorum.
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            ReaderCommandCount++;
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }
    }

    // Burada senaryoda kullanılan kalıcı kimlikleri tek test kaydında taşıyorum.
    private sealed record FavoriteSeed(
        long FirstProductId,
        long SecondProductId,
        Guid FirstVariantId,
        Guid SecondVariantId,
        long FirstUserId,
        long SecondUserId);

    // Burada ürünün favori summary, popularity, satır ve event metriklerini birlikte taşıyorum.
    private sealed record ProductFavoriteState(
        long FavoriteCount,
        long PopularityScore,
        int FavoriteRows,
        long DailyFavoriteCount);

    // Burada aynı ürünün user ve guest owner kayıt sayılarını taşıyorum.
    private sealed record FavoriteOwnershipState(int UserRows, int GuestRows);

    // Burada claim sonrasında iki owner türündeki cart ve favorite kayıt sayılarını taşıyorum.
    private sealed record OwnerState(
        int UserCartRows,
        int GuestCartRows,
        int UserFavoriteRows,
        int GuestFavoriteRows);
}
