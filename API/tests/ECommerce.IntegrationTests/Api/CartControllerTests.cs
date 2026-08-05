using System.Security.Claims;
using ECommerce.API.Controllers.Cart;
using ECommerce.Application.Carts.Commands.AddCartItem;
using ECommerce.Application.Carts.Commands.MergeGuestCart;
using ECommerce.Application.Carts.Dtos;
using ECommerce.Application.Carts.Queries.GetCart;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace ECommerce.IntegrationTests.Api;

public sealed class CartControllerTests
{
    // Burada anonim ekleme isteğinin güvenli cookie üretip yalnız kabul edilen alanları Application katmanına ilettiğini doğruluyorum.
    [Fact]
    public async Task AddItem_Should_Create_Secure_Guest_Cookie_And_Send_Trusted_Command()
    {
        var sender = new RecordingSender();
        var controller = CreateController(sender);
        var variantId = Guid.NewGuid();

        var result = await controller.AddItem(
            new AddCartItemRequest(variantId, 2),
            CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>();
        var command = sender.LastRequest.Should().BeOfType<AddCartItemCommand>().Subject;
        command.ProductVariantId.Should().Be(variantId);
        command.Quantity.Should().Be(2);
        command.SessionId.Should().NotBeNull();
        command.SessionId.Should().MatchRegex("^[0-9A-F]{64}$");
        controller.Response.Headers.SetCookie.Should().ContainSingle();
        controller.Response.Headers.SetCookie.Single().Should().Contain("ecommerce_guest_cart=");
        controller.Response.Headers.SetCookie.Single().Should().Contain("httponly");
        controller.Response.Headers.SetCookie.Single().Should().Contain("secure");
        controller.Response.Headers.SetCookie.Single().Should().Contain("samesite=lax");
    }

    // Burada giriş yapmış kullanıcının sepetinde misafir cookie değerinin Application katmanına taşınmadığını doğruluyorum.
    [Fact]
    public async Task GetCart_Should_Not_Use_Guest_Cookie_For_Authenticated_User()
    {
        var sender = new RecordingSender();
        var controller = CreateController(sender, isAuthenticated: true);
        controller.Request.Headers.Cookie = "ecommerce_guest_cart=" + new string('A', 64);

        var result = await controller.GetCart(CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>();
        var query = sender.LastRequest.Should().BeOfType<GetCartQuery>().Subject;
        query.SessionId.Should().BeNull();
        controller.Response.Headers.SetCookie.Should().BeEmpty();
    }

    // Burada giriş sonrasında geçerli misafir cookie'sinin merge komutuna aktarıldığını ve işlem sonunda silindiğini doğruluyorum.
    [Fact]
    public async Task MergeGuestCart_Should_Use_Existing_Cookie_And_Delete_It_After_Success()
    {
        var sender = new RecordingSender();
        var controller = CreateController(sender, isAuthenticated: true);
        var guestSessionId = new string('B', 64);
        controller.Request.Headers.Cookie = "ecommerce_guest_cart=" + guestSessionId;

        var result = await controller.MergeGuestCart(CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>();
        var command = sender.LastRequest.Should().BeOfType<MergeGuestCartCommand>().Subject;
        command.SessionId.Should().Be(guestSessionId);
        controller.Response.Headers.SetCookie.Should().ContainSingle();
        controller.Response.Headers.SetCookie.Single().Should().Contain("ecommerce_guest_cart=");
        controller.Response.Headers.SetCookie.Single().Should().Contain("expires=");
    }

    // Burada controller testleri için HttpContext ve isteğe bağlı doğrulanmış kullanıcı oluşturarak controller'ı hazırlıyorum.
    private static CartController CreateController(RecordingSender sender, bool isAuthenticated = false)
    {
        var httpContext = new DefaultHttpContext();
        if (isAuthenticated)
        {
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, "U00001")],
                authenticationType: "Test"));
        }

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["GuestProtection:TrustedOrigins"] = "https://store.example.test"
            })
            .Build();
        return new CartController(sender, configuration)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            }
        };
    }

    private sealed class RecordingSender : ISender
    {
        public object? LastRequest { get; private set; }

        // Burada cevap dönen MediatR isteklerini kaydedip Cart testleri için boş sepet cevabı veriyorum.
        public Task<TResponse> Send<TResponse>(
            IRequest<TResponse> request,
            CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return Task.FromResult((TResponse)(object)CartDto.Empty());
        }

        // Burada cevapsız MediatR isteklerini kaydedip başarılı tamamlanma taklit ediyorum.
        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest
        {
            LastRequest = request;
            return Task.CompletedTask;
        }

        // Burada dinamik MediatR isteklerini kaydedip Cart testleri için boş sepet cevabı veriyorum.
        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return Task.FromResult<object?>(CartDto.Empty());
        }

        // Burada generic stream istekleri için boş bir akış dönüyorum.
        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
            IStreamRequest<TResponse> request,
            CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return EmptyAsync<TResponse>();
        }

        // Burada dinamik stream istekleri için boş bir akış dönüyorum.
        public IAsyncEnumerable<object?> CreateStream(
            object request,
            CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return EmptyAsync<object?>();
        }

        // Burada stream test yardımcılarının boş asenkron koleksiyon üretmesini sağlıyorum.
        private static async IAsyncEnumerable<T> EmptyAsync<T>()
        {
            await Task.CompletedTask;
            yield break;
        }
    }
}
