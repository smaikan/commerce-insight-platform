using ECommerce.API.Controllers.Shipping;
using ECommerce.API.Controllers.Tax;
using ECommerce.API.Security;
using ECommerce.Application.Common.Models;
using ECommerce.Application.ShippingMethods.Commands.CreateShippingMethod;
using ECommerce.Application.ShippingMethods.Dtos;
using ECommerce.Application.ShippingMethods.Queries.GetShippingMethods;
using ECommerce.Application.TaxRates.Dtos;
using ECommerce.Application.TaxRates.Queries.GetTaxRates;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.IntegrationTests.Api;

public sealed class TaxAndShippingControllersTests
{
    // Burada herkese açık vergi oranı listesinin yalnız aktif kayıt filtresini sorguya taşıdığını doğruluyorum.
    [Fact]
    public async Task TaxRateActiveList_Should_Send_Active_Query_And_Allow_Anonymous_Access()
    {
        var sender = new RecordingSender();
        var controller = new TaxRatesController(sender);

        var result = await controller.GetActiveList(2, 50, CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>();
        var query = sender.LastRequest.Should().BeOfType<GetTaxRatesQuery>().Subject;
        query.PageNumber.Should().Be(2);
        query.PageSize.Should().Be(50);
        query.IsActive.Should().BeTrue();
        typeof(TaxRatesController)
            .GetMethod(nameof(TaxRatesController.GetActiveList))!
            .GetCustomAttributes(inherit: true)
            .OfType<AllowAnonymousAttribute>()
            .Should()
            .ContainSingle();
    }

    // Burada kargo yöntemi oluşturma endpointinin HTTP isteğini doğru Application komutuna çevirdiğini doğruluyorum.
    [Fact]
    public async Task ShippingMethodCreate_Should_Send_Create_Command_And_Return_Created()
    {
        var sender = new RecordingSender();
        var controller = new ShippingMethodsController(sender);
        var request = new CreateShippingMethodRequest("Ekspres", 99.90m, true, 2);

        var result = await controller.Create(request, CancellationToken.None);

        result.Result.Should().BeOfType<CreatedAtActionResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status201Created);
        var command = sender.LastRequest.Should().BeOfType<CreateShippingMethodCommand>().Subject;
        command.Name.Should().Be("Ekspres");
        command.FixedFee.Should().Be(99.90m);
        command.DisplayOrder.Should().Be(2);
    }

    // Burada yönetim uçlarının AdminOnly politikasıyla korunduğunu doğruluyorum.
    [Fact]
    public void Controllers_Should_Require_AdminOnly_Policy()
    {
        var taxAuthorization = typeof(TaxRatesController)
            .GetCustomAttributes(inherit: true)
            .OfType<AuthorizeAttribute>()
            .Single();
        var shippingAuthorization = typeof(ShippingMethodsController)
            .GetCustomAttributes(inherit: true)
            .OfType<AuthorizeAttribute>()
            .Single();

        taxAuthorization.Policy.Should().Be(AuthorizationPolicies.AdminOnly);
        shippingAuthorization.Policy.Should().Be(AuthorizationPolicies.AdminOnly);
    }

    private sealed class RecordingSender : ISender
    {
        public object? LastRequest { get; private set; }

        // Burada cevabı olan MediatR isteğini kaydedip test için uygun cevap modelini döndürüyorum.
        public Task<TResponse> Send<TResponse>(
            IRequest<TResponse> request,
            CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return Task.FromResult(CreateResponse<TResponse>());
        }

        // Burada cevapsız MediatR isteğini kaydedip başarılı tamamlanma taklidi yapıyorum.
        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest
        {
            LastRequest = request;
            return Task.CompletedTask;
        }

        // Burada dinamik MediatR isteğini kaydedip temsilî cevap modelini döndürüyorum.
        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return Task.FromResult<object?>(CreateShippingMethodDto());
        }

        // Burada generic stream istekleri için eleman üretmeyen asenkron akış sağlıyorum.
        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
            IStreamRequest<TResponse> request,
            CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return EmptyAsync<TResponse>();
        }

        // Burada dinamik stream istekleri için eleman üretmeyen asenkron akış sağlıyorum.
        public IAsyncEnumerable<object?> CreateStream(
            object request,
            CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return EmptyAsync<object?>();
        }

        // Burada istenen cevap türüne göre temsilî vergi veya kargo DTO'su üretiyorum.
        private static TResponse CreateResponse<TResponse>()
        {
            if (typeof(TResponse) == typeof(ShippingMethodDto))
            {
                return (TResponse)(object)CreateShippingMethodDto();
            }

            if (typeof(TResponse) == typeof(TaxRateDto))
            {
                return (TResponse)(object)CreateTaxRateDto();
            }

            if (typeof(TResponse) == typeof(PagedResult<TaxRateDto>))
            {
                return (TResponse)(object)new PagedResult<TaxRateDto>([CreateTaxRateDto()], 1, 20, 1);
            }

            if (typeof(TResponse) == typeof(PagedResult<ShippingMethodDto>))
            {
                return (TResponse)(object)new PagedResult<ShippingMethodDto>([CreateShippingMethodDto()], 1, 20, 1);
            }

            throw new InvalidOperationException($"Unexpected response type {typeof(TResponse).Name}.");
        }

        // Burada controller cevapları için temsilî vergi oranı DTO'su hazırlıyorum.
        private static TaxRateDto CreateTaxRateDto()
        {
            return new TaxRateDto(Guid.NewGuid(), "KDV", 20m, true, DateTime.UtcNow, null);
        }

        // Burada controller cevapları için temsilî kargo yöntemi DTO'su hazırlıyorum.
        private static ShippingMethodDto CreateShippingMethodDto()
        {
            return new ShippingMethodDto(Guid.NewGuid(), "Standart", 49.90m, true, 1, DateTime.UtcNow, null);
        }

        // Burada stream testleri için gerçekten eleman üretmeyen asenkron koleksiyon hazırlıyorum.
        private static async IAsyncEnumerable<T> EmptyAsync<T>()
        {
            await Task.CompletedTask;
            yield break;
        }
    }
}
