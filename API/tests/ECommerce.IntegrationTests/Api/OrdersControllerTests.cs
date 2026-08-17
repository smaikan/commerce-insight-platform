using System.Security.Claims;
using ECommerce.API.Controllers.Order;
using ECommerce.Application.Orders.Commands.CreateOrder;
using ECommerce.Application.Orders.Dtos;
using ECommerce.Application.Payments;
using ECommerce.Domain.Enums;
using ECommerce.Infrastructure.Payments;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;

namespace ECommerce.IntegrationTests.Api;

public sealed class OrdersControllerTests
{
    // Burada checkout endpointinin yalnız concurrency tokenı komuta taşıyıp 201 sipariş cevabı verdiğini doğruluyorum.
    [Fact]
    public async Task Create_Should_Send_Checkout_Command_And_Return_Created()
    {
        var sender = new RecordingSender();
        var controller = new OrdersController(sender)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        [new Claim(ClaimTypes.NameIdentifier, "U00001")],
                        authenticationType: "Test"))
                }
            }
        };
        var token = Guid.NewGuid();
        var shippingAddressId = Guid.NewGuid();
        var shippingMethodId = Guid.NewGuid();

        var result = await controller.Create(
            new CreateOrderRequest(token, shippingAddressId, shippingMethodId),
            CancellationToken.None);

        result.Result.Should().BeOfType<CreatedAtActionResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status201Created);
        sender.LastRequest.Should().BeOfType<CreateOrderCommand>()
            .Which.Should().Match<CreateOrderCommand>(command =>
                command.ExpectedCartConcurrencyToken == token &&
                command.ShippingAddressId == shippingAddressId &&
                command.ShippingMethodId == shippingMethodId);
    }

    // Burada ödeme endpointinin idempotency header değerini istemci gövdesinden bağımsız olarak güvenli komuta taşıdığını doğruluyorum.
    [Fact]
    public async Task CreatePayment_Should_Send_Idempotency_Header_In_Command()
    {
        var sender = new RecordingSender();
        var controller = new OrdersController(sender);
        var orderId = Guid.NewGuid();
        const string idempotencyKey = "payment_header_key_0001";

        var result = await controller.CreatePayment(
            orderId,
            new CreatePaymentRequest(PaymentProvider.Fake),
            idempotencyKey,
            CancellationToken.None);

        result.Result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status201Created);
        sender.LastRequest.Should().BeOfType<ECommerce.Application.Orders.Commands.CreatePayment.CreatePaymentCommand>()
            .Which.IdempotencyKey.Should().Be(idempotencyKey);
    }

    // Burada iyzico form endpointinin kart verisi almadan header ve istemci IP'sini komuta taşıdığını doğruluyorum.
    [Fact]
    public async Task InitializeIyzicoCheckoutForm_Should_Send_Server_Controlled_Command()
    {
        var sender = new RecordingSender();
        var controller = new OrdersController(sender)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
        controller.HttpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Loopback;
        var orderId = Guid.NewGuid();

        var result = await controller.InitializeIyzicoCheckoutForm(
            orderId,
            "iyzico_controller_key_0001",
            CancellationToken.None);

        result.Result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status201Created);
        sender.LastRequest.Should().BeOfType<InitializeIyzicoCheckoutFormCommand>()
            .Which.Should().Match<InitializeIyzicoCheckoutFormCommand>(command =>
                command.OrderId == orderId &&
                command.IdempotencyKey == "iyzico_controller_key_0001" &&
                command.ClientIpAddress == "127.0.0.1");
    }

    // Burada kargo alanlarının admin durum endpointinden tip güvenli komuta eksiksiz taşındığını doğruluyorum.
    [Fact]
    public async Task ChangeStatus_Should_Send_Shipment_Fields_In_Command()
    {
        var sender = new RecordingSender();
        var controller = new OrdersController(sender);
        var orderId = Guid.NewGuid();

        var result = await controller.ChangeStatus(
            orderId,
            new ChangeOrderStatusRequest(
                OrderStatus.Shipped,
                "Carrier",
                "TRACK-123",
                "https://track.example.com/TRACK-123"),
            CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>();
        sender.LastRequest.Should().BeOfType<ECommerce.Application.Orders.Commands.ChangeOrderStatus.ChangeOrderStatusCommand>()
            .Which.Should().Match<ECommerce.Application.Orders.Commands.ChangeOrderStatus.ChangeOrderStatusCommand>(command =>
                command.OrderId == orderId &&
                command.Status == OrderStatus.Shipped &&
                command.ShippingCarrier == "Carrier" &&
                command.TrackingNumber == "TRACK-123" &&
                command.TrackingUrl == "https://track.example.com/TRACK-123");
    }

    // Burada sahte ödeme sağlayıcısının production ortamında siparişi ödenmiş duruma geçirecek başarı sonucu üretmediğini doğruluyorum.
    [Fact]
    public async Task FakePaymentGateway_Should_Be_Disabled_In_Production()
    {
        var gateway = new FakePaymentGateway(new StubHostEnvironment("Production"));

        var result = await gateway.ChargeAsync(
            new ECommerce.Application.Common.Payments.PaymentGatewayRequest(
                Guid.NewGuid(),
                10m,
                "production_fake_payment_01"));

        result.Succeeded.Should().BeFalse();
        result.TransactionId.Should().BeNull();
    }

    // Burada sahte ödeme sağlayıcısının yalnız geliştirme ortamında kontrollü başarılı sonuç ürettiğini doğruluyorum.
    [Fact]
    public async Task FakePaymentGateway_Should_Work_In_Development()
    {
        var gateway = new FakePaymentGateway(new StubHostEnvironment("Development"));

        var result = await gateway.ChargeAsync(
            new ECommerce.Application.Common.Payments.PaymentGatewayRequest(
                Guid.NewGuid(),
                10m,
                "development_fake_payment_01"));

        result.Succeeded.Should().BeTrue();
        result.TransactionId.Should().StartWith("fake_");
    }

    private sealed class RecordingSender : ISender
    {
        public object? LastRequest { get; private set; }

        // Burada cevaplı MediatR isteğini kaydedip controller testine örnek sipariş cevabı veriyorum.
        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            if (typeof(TResponse) == typeof(PaymentDto))
            {
                return Task.FromResult((TResponse)(object)new PaymentDto(
                    Guid.NewGuid(),
                    PaymentProvider.Fake,
                    PaymentStatus.Paid,
                    10m,
                    "fake_transaction_controller_001",
                    DateTime.UtcNow,
                    DateTime.UtcNow));
            }

            if (typeof(TResponse) == typeof(CheckoutFormSessionDto))
            {
                return Task.FromResult((TResponse)(object)new CheckoutFormSessionDto(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    PaymentProvider.Iyzico,
                    PaymentStatus.Pending,
                    10m,
                    "https://sandbox-api.iyzipay.com/checkoutform/test",
                    DateTime.UtcNow.AddMinutes(30)));
            }

            var response = new OrderDto(
                    Guid.NewGuid(),
                    "ORD-TEST",
                    OrderStatus.Pending,
                    10m,
                    0m,
                    0m,
                    0m,
                    10m,
                    null,
                    null,
                    [],
                    [],
                    null,
                    null,
                    null,
                    null,
                    DateTime.UtcNow);
            return Task.FromResult((TResponse)(object)response);
        }

        // Burada cevapsız MediatR isteğini kaydedip başarıyla tamamlıyorum.
        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest
        {
            LastRequest = request;
            return Task.CompletedTask;
        }

        // Burada dinamik MediatR isteğini kaydedip örnek sipariş cevabı döndürüyorum.
        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return Task.FromResult<object?>(null);
        }

        // Burada generic stream istekleri için boş asenkron akış üretiyorum.
        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return EmptyAsync<TResponse>();
        }

        // Burada dinamik stream istekleri için boş asenkron akış üretiyorum.
        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return EmptyAsync<object?>();
        }

        // Burada test stream yardımcıları için boş koleksiyon sağlıyorum.
        private static async IAsyncEnumerable<T> EmptyAsync<T>()
        {
            await Task.CompletedTask;
            yield break;
        }
    }

    private sealed class StubHostEnvironment : IHostEnvironment
    {
        // Burada ödeme adapter testinin ortam adını güvenli biçimde sabitliyorum.
        public StubHostEnvironment(string environmentName)
        {
            EnvironmentName = environmentName;
        }

        public string EnvironmentName { get; set; }
        public string ApplicationName { get; set; } = "ECommerce.Tests";
        public string ContentRootPath { get; set; } = string.Empty;
        public string? WebRootPath { get; set; }
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
        public Microsoft.Extensions.FileProviders.IFileProvider WebRootFileProvider { get; set; } = null!;
    }
}
