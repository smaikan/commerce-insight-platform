using ECommerce.API.Controllers.Coupon;
using ECommerce.API.Security;
using ECommerce.Application.Common.Models;
using ECommerce.Application.Coupons.Commands.CreateCoupon;
using ECommerce.Application.Coupons.Commands.SetCouponActivation;
using ECommerce.Application.Coupons.Commands.UpdateCoupon;
using ECommerce.Application.Coupons.Dtos;
using ECommerce.Application.Coupons.Queries.GetCoupons;
using ECommerce.Domain.Enums;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.IntegrationTests.Api;

public sealed class CouponsControllerTests
{
    // Burada kupon oluşturma endpointinin yönetim HTTP isteğini oluşturma komutuna çevirdiğini doğruluyorum.
    [Fact]
    public async Task Create_Should_Send_Create_Command_And_Return_Created()
    {
        var sender = new RecordingSender();
        var controller = CreateController(sender);
        var request = CreateRequest();

        var result = await controller.Create(request, CancellationToken.None);

        result.Result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status201Created);
        var command = sender.LastRequest.Should().BeOfType<CreateCouponCommand>().Subject;
        command.Code.Should().Be("SUMMER20");
        command.DiscountType.Should().Be(CouponDiscountType.Percentage);
        command.DiscountValue.Should().Be(20m);
    }

    // Burada kupon güncelleme endpointinin rota kimliğini update komutuna taşıdığını doğruluyorum.
    [Fact]
    public async Task Update_Should_Send_Route_Coupon_Id_In_Command()
    {
        var sender = new RecordingSender();
        var controller = CreateController(sender);
        var couponId = Guid.NewGuid();

        var result = await controller.Update(couponId, CreateRequest(), CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>();
        sender.LastRequest.Should().BeOfType<UpdateCouponCommand>()
            .Which.Id.Should().Be(couponId);
    }

    // Burada kupon aktiflik endpointinin rota kimliği ve gövde değerini activation komutuna aktardığını doğruluyorum.
    [Fact]
    public async Task SetActivation_Should_Send_Activation_Command()
    {
        var sender = new RecordingSender();
        var controller = CreateController(sender);
        var couponId = Guid.NewGuid();

        var result = await controller.SetActivation(
            couponId,
            new SetCouponActivationRequest(false),
            CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>();
        var command = sender.LastRequest.Should().BeOfType<SetCouponActivationCommand>().Subject;
        command.Id.Should().Be(couponId);
        command.IsActive.Should().BeFalse();
    }

    // Burada kupon listeleme endpointinin sayfalama ve aktiflik filtresini sorguya aktardığını doğruluyorum.
    [Fact]
    public async Task GetList_Should_Send_Paging_And_Activation_Filter()
    {
        var sender = new RecordingSender();
        var controller = CreateController(sender);

        var result = await controller.GetList(2, 50, false, CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>();
        var query = sender.LastRequest.Should().BeOfType<GetCouponsQuery>().Subject;
        query.PageNumber.Should().Be(2);
        query.PageSize.Should().Be(50);
        query.IsActive.Should().BeFalse();
    }

    // Burada kupon yönetim controllerının tüm uçlarını yalnız AdminOnly politikasına kapattığını doğruluyorum.
    [Fact]
    public void Controller_Should_Require_Admin_Policy()
    {
        var authorization = typeof(CouponsController)
            .GetCustomAttributes(inherit: true)
            .OfType<AuthorizeAttribute>()
            .Single();

        authorization.Policy.Should().Be(AuthorizationPolicies.AdminOnly);
    }

    // Burada doğrudan controller testleri için sender bağlı kupon controllerını hazırlıyorum.
    private static CouponsController CreateController(RecordingSender sender)
    {
        return new CouponsController(sender);
    }

    // Burada testler için geçerli kupon yönetim HTTP isteğini hazırlıyorum.
    private static CouponRequest CreateRequest()
    {
        return new CouponRequest(
            "SUMMER20",
            CouponDiscountType.Percentage,
            20m,
            "Yaz indirimi",
            100m,
            1000,
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(7),
            true);
    }

    private sealed class RecordingSender : ISender
    {
        public object? LastRequest { get; private set; }

        // Burada cevaplı MediatR isteklerini kaydedip endpoint testleri için uygun kupon cevabı veriyorum.
        public Task<TResponse> Send<TResponse>(
            IRequest<TResponse> request,
            CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return Task.FromResult(CreateResponse<TResponse>());
        }

        // Burada cevapsız MediatR isteklerini kaydedip başarılı tamamlanma taklit ediyorum.
        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest
        {
            LastRequest = request;
            return Task.CompletedTask;
        }

        // Burada dinamik MediatR isteğini kaydedip örnek kupon cevabı döndürüyorum.
        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return Task.FromResult<object?>(CreateCouponDto());
        }

        // Burada generic stream istekleri için boş asenkron akış üretiyorum.
        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
            IStreamRequest<TResponse> request,
            CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return EmptyAsync<TResponse>();
        }

        // Burada dinamik stream istekleri için boş asenkron akış üretiyorum.
        public IAsyncEnumerable<object?> CreateStream(
            object request,
            CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return EmptyAsync<object?>();
        }

        // Burada istenen response türüne göre tekil veya sayfalı kupon cevabını üretiyorum.
        private static TResponse CreateResponse<TResponse>()
        {
            if (typeof(TResponse) == typeof(CouponDto))
            {
                return (TResponse)(object)CreateCouponDto();
            }

            if (typeof(TResponse) == typeof(PagedResult<CouponDto>))
            {
                var response = new PagedResult<CouponDto>([CreateCouponDto()], 1, 20, 1);
                return (TResponse)(object)response;
            }

            throw new InvalidOperationException($"Unexpected response type {typeof(TResponse).Name}.");
        }

        // Burada controller cevapları için temsilî kupon DTO'su hazırlıyorum.
        private static CouponDto CreateCouponDto()
        {
            return new CouponDto(
                Guid.NewGuid(),
                "SUMMER20",
                "Yaz indirimi",
                CouponDiscountType.Percentage,
                20m,
                100m,
                1000,
                0,
                DateTime.UtcNow,
                DateTime.UtcNow.AddDays(7),
                true,
                false,
                DateTime.UtcNow,
                null);
        }

        // Burada stream testleri için gerçekten eleman üretmeyen asenkron koleksiyon hazırlıyorum.
        private static async IAsyncEnumerable<T> EmptyAsync<T>()
        {
            await Task.CompletedTask;
            yield break;
        }
    }
}
