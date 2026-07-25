using ECommerce.Application.Orders.Commands.CreateOrder;
using ECommerce.Application.Orders.Commands.CreatePayment;
using ECommerce.Application.Orders.Commands.CancelOrder;
using ECommerce.Application.Orders.Commands.ChangeOrderStatus;
using ECommerce.Application.Orders.Dtos;
using ECommerce.Application.Orders.Queries.GetMyOrders;
using ECommerce.Application.Orders.Queries.GetOrderById;
using ECommerce.Application.Orders.Queries.GetOrderByIdForAdmin;
using ECommerce.Application.Orders.Queries.GetOrders;
using ECommerce.API.Security;
using ECommerce.Application.Common.Models;
using ECommerce.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

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

    // Burada oturumdaki kullanıcının kendi sipariş özetlerini sayfalı olarak getiriyorum.
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

    // Burada kullanıcının yalnız ödeme öncesi kendi siparişini iptal etmesini sağlıyorum.
    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<OrderDto>> Cancel(Guid id, CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new CancelOrderCommand(id), cancellationToken));

    // Burada yöneticinin tüm siparişleri güvenli filtrelerle sayfalı olarak görmesini sağlıyorum.
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
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
        Ok(await _sender.Send(new ChangeOrderStatusCommand(id, request.Status), cancellationToken));
}

// Burada checkout isteğinin concurrency, isteğe bağlı teslimat adresi ve kupon alanlarını tanımlıyorum.
public sealed record CreateOrderRequest(
    Guid ExpectedCartConcurrencyToken,
    Guid? ShippingAddressId = null,
    string? CouponCode = null,
    Guid? ShippingMethodId = null);

// Burada ödeme başlatma gövdesinden kabul edilen tek sağlayıcı seçimini tanımlıyorum.
public sealed record CreatePaymentRequest(PaymentProvider Provider);

// Burada yönetim sipariş yaşam döngüsü güncellemesi için hedef durumu tanımlıyorum.
public sealed record ChangeOrderStatusRequest(OrderStatus Status);
