using ECommerce.Application.Orders.Commands.CreateOrder;
using ECommerce.Application.Orders.Commands.CreatePayment;
using ECommerce.Application.Orders.Commands.CancelOrder;
using ECommerce.Application.Orders.Commands.ChangeOrderStatus;
using ECommerce.Application.Orders.Commands.ImportOrders;
using ECommerce.Application.Orders.Dtos;
using ECommerce.Application.Orders.Queries.GetMyOrders;
using ECommerce.Application.Orders.Queries.GetOrderById;
using ECommerce.Application.Orders.Queries.GetOrderByIdForAdmin;
using ECommerce.Application.Orders.Queries.GetOrders;
using ECommerce.Application.Orders.Queries.GetOrderCancellation;
using ECommerce.API.Security;
using ECommerce.API.Routing;
using ECommerce.Application.Common.Models;
using ECommerce.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.ComponentModel.DataAnnotations;
using ECommerce.Application.Payments;

namespace ECommerce.API.Controllers.Order;

[ApiController]
[Authorize]
[EnableRateLimiting("orders")]
[Route("api/orders")]
public sealed class OrdersController : ControllerBase
{
    private readonly ISender _sender;

    // Burada sipariş HTTP isteklerini Application katmanına yönlendirecek sender'ı hazırlıyorum.
    public OrdersController(ISender sender)
    {
        _sender = sender;
    }

    // Burada oturum açmış kullanıcının güncel sepetini checkout akışına gönderip yeni siparişi oluşturuyorum.
    [HttpPost]
    public async Task<ActionResult<OrderDto>> Create(
        CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        var order = await _sender.Send(
            new CreateOrderCommand(
                request.ExpectedCartConcurrencyToken,
                request.ShippingAddressId,
                request.CouponCode,
                request.ShippingMethodId),
            cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
    }

    // Burada dış sistemden tek siparişi admin yetkisiyle, sipariş numarasını idempotency anahtarı kabul ederek içe aktarıyorum.
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [HttpPost("import")]
    public async Task<ActionResult<OrderImportResultDto>> Import(
        ImportOrderRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ImportOrderCommand(request.ToInput()), cancellationToken);
        return result.WasImported
            ? StatusCode(StatusCodes.Status201Created, result)
            : Ok(result);
    }

    // Burada dış sistemden gelen siparişleri tek transaction içinde atomik ve tekrar güvenli olarak topluca içe aktarıyorum.
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [HttpPost("import/bulk")]
    public async Task<ActionResult<IReadOnlyList<OrderImportResultDto>>> BulkImport(
        BulkImportOrdersRequest request,
        CancellationToken cancellationToken)
    {
        var results = await _sender.Send(
            new BulkImportOrdersCommand(request.Orders.Select(order => order.ToInput()).ToList()),
            cancellationToken);
        return StatusCode(StatusCodes.Status201Created, results);
    }

    // Burada oturumdaki kullanıcının kendi sipariş özetlerini sayfalı olarak getiriyorum.
    [DisableRateLimiting]
    [HttpGet("mine")]
    public async Task<ActionResult<PagedResult<OrderSummaryDto>>> GetMine(
        [FromQuery] GetMyOrdersQuery query,
        CancellationToken cancellationToken) =>
        Ok(await _sender.Send(query, cancellationToken));

    // Burada oturumdaki kullanıcının yalnız kendi sipariş detayını getiriyorum.
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderDto>> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new GetOrderByIdQuery(id), cancellationToken));

    // Burada istemcinin idempotency header'ıyla güvenli ödeme denemesi başlatmasını sağlıyorum.
    [EnableRateLimiting("payments")]
    [HttpPost("{id:guid}/payments")]
    public async Task<ActionResult<PaymentDto>> CreatePayment(
        Guid id,
        CreatePaymentRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var payment = await _sender.Send(
            new CreatePaymentCommand(id, request.Provider, idempotencyKey ?? string.Empty),
            cancellationToken);
        return StatusCode(StatusCodes.Status201Created, payment);
    }

    // Burada kart verisini API'ye almadan üyenin iyzico hosted ödeme sayfasını başlatıyorum.
    [EnableRateLimiting("payments")]
    [HttpPost("{id:guid}/payments/iyzico/checkout-form")]
    [ProducesResponseType<CheckoutFormSessionDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CheckoutFormSessionDto>> InitializeIyzicoCheckoutForm(
        Guid id,
        [FromHeader(Name = "Idempotency-Key"), Required] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new InitializeIyzicoCheckoutFormCommand(
                id,
                idempotencyKey ?? string.Empty,
                HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1"),
            cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    // Burada kullanıcının Shipped öncesi siparişini doğrudan veya provider reversal sagasıyla iptal etmesini sağlıyorum.
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType<OrderDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<OrderCancellationOperationDto>(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new CancelOrderCommand(id), cancellationToken);
        return result.IsCompleted
            ? Ok(result.Order)
            : Accepted(result.Operation);
    }

    // Burada üyenin yalnız kendi siparişindeki devam eden cancellation operasyonunu polling için getiriyorum.
    [HttpGet("{id:guid}/cancellation")]
    [ProducesResponseType<OrderCancellationOperationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderCancellationOperationDto>> GetCancellation(
        Guid id,
        CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new GetOrderCancellationQuery(id), cancellationToken));

    // Burada yöneticinin tüm siparişleri güvenli filtrelerle sayfalı olarak görmesini sağlıyorum.
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [DisableRateLimiting]
    [HttpGet]
    public async Task<ActionResult<PagedResult<OrderSummaryDto>>> GetList(
        [FromQuery] GetOrdersQuery query,
        CancellationToken cancellationToken) =>
        Ok(await _sender.Send(query, cancellationToken));

    // Burada yöneticinin seçili siparişin ayrıntılı durum ve teslimat snapshot'ını görmesini sağlıyorum.
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [HttpGet("admin/{id:guid}")]
    public async Task<ActionResult<OrderDto>> GetByIdForAdmin(Guid id, CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new GetOrderByIdForAdminQuery(id), cancellationToken));

    // Burada yöneticinin geçerli sipariş yaşam döngüsü durumunu değiştirmesini sağlıyorum.
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<OrderDto>> ChangeStatus(
        Guid id,
        ChangeOrderStatusRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _sender.Send(
            new ChangeOrderStatusCommand(
                id,
                request.Status,
                request.ShippingCarrier,
                request.TrackingNumber,
                request.TrackingUrl),
            cancellationToken));
}

// Burada checkout isteğinin concurrency, isteğe bağlı teslimat adresi ve kupon alanlarını tanımlıyorum.
public sealed record CreateOrderRequest(
    Guid ExpectedCartConcurrencyToken,
    Guid ShippingAddressId,
    Guid ShippingMethodId,
    string? CouponCode = null);

// Burada ödeme başlatma gövdesinden kabul edilen tek sağlayıcı seçimini tanımlıyorum.
public sealed record CreatePaymentRequest(PaymentProvider Provider);

// Burada yönetim sipariş yaşam döngüsü güncellemesi için hedef durumu tanımlıyorum.
public sealed record ChangeOrderStatusRequest(
    OrderStatus Status,
    [property: MaxLength(ECommerce.Domain.Entities.Order.MaximumShippingCarrierLength)] string? ShippingCarrier = null,
    [property: MaxLength(ECommerce.Domain.Entities.Order.MaximumTrackingNumberLength)] string? TrackingNumber = null,
    [property: MaxLength(ECommerce.Domain.Entities.Order.MaximumTrackingUrlLength), Url] string? TrackingUrl = null);

public sealed record BulkImportOrdersRequest(IReadOnlyList<ImportOrderRequest> Orders);

public sealed record ImportOrderRequest(
    string OrderNumber,
    long UserId,
    decimal SubTotal,
    decimal DiscountTotal,
    decimal ShippingTotal,
    decimal TaxTotal,
    decimal GrandTotal,
    OrderStatus Status,
    IReadOnlyList<ImportOrderItemRequest> Items,
    DateTime? CreatedAtUtc = null,
    string? CouponCode = null,
    Guid? ShippingMethodId = null,
    string? ShippingMethodName = null,
    PaymentProvider? PaymentProvider = null,
    string? PaymentTransactionId = null,
    bool ApplyInventoryAndMetrics = false)
{
    public ImportedOrderInput ToInput() => new(
        OrderNumber,
        UserId,
        SubTotal,
        DiscountTotal,
        ShippingTotal,
        TaxTotal,
        GrandTotal,
        Status,
        Items.Select(item => item.ToInput()).ToList(),
        CreatedAtUtc,
        CouponCode,
        ShippingMethodId,
        ShippingMethodName,
        PaymentProvider,
        PaymentTransactionId,
        ApplyInventoryAndMetrics);
}

public sealed record ImportOrderItemRequest(
    string ProductId,
    Guid ProductVariantId,
    string ProductTitle,
    string VariantSku,
    decimal UnitPrice,
    int Quantity,
    decimal DiscountTotal = 0m,
    decimal TaxRatePercentage = 0m,
    decimal TaxTotal = 0m)
{
    public ImportedOrderItemInput ToInput() => new(
        ApiPublicIdParser.ParseProductId(ProductId),
        ProductVariantId,
        ProductTitle,
        VariantSku,
        UnitPrice,
        Quantity,
        DiscountTotal,
        TaxRatePercentage,
        TaxTotal);
}
