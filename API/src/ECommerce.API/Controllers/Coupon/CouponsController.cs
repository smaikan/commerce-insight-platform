using ECommerce.API.Security;
using ECommerce.Application.Common.Models;
using ECommerce.Application.Coupons.Commands.CreateCoupon;
using ECommerce.Application.Coupons.Commands.SetCouponActivation;
using ECommerce.Application.Coupons.Commands.UpdateCoupon;
using ECommerce.Application.Coupons.Dtos;
using ECommerce.Application.Coupons.Queries.GetCoupons;
using ECommerce.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers.Coupon;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
[Route("api/coupons")]
public sealed class CouponsController : ControllerBase
{
    private readonly ISender _sender;

    // Burada yönetici kupon HTTP isteklerini Application katmanına yönlendirecek sender'ı hazırlıyorum.
    public CouponsController(ISender sender)
    {
        _sender = sender;
    }

    // Burada yöneticinin sayfalı ve isteğe bağlı aktiflik filtreli kupon listesini istemesini iletiyorum.
    [HttpGet]
    public async Task<ActionResult<PagedResult<CouponDto>>> GetList(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var coupons = await _sender.Send(
            new GetCouponsQuery(pageNumber, pageSize, isActive),
            cancellationToken);
        return Ok(coupons);
    }

    // Burada yönetici tarafından gelen yeni kupon bilgilerini oluşturma komutuna aktarıyorum.
    [HttpPost]
    public async Task<ActionResult<CouponDto>> Create(
        CouponRequest request,
        CancellationToken cancellationToken)
    {
        var coupon = await _sender.Send(
            new CreateCouponCommand(
                request.Code,
                request.DiscountType,
                request.DiscountValue,
                request.Description,
                request.MinimumOrderAmount,
                request.UsageLimit,
                request.StartsAt,
                request.ExpiresAt,
                request.IsActive,
                request.IsMemberOnly),
            cancellationToken);
        return StatusCode(StatusCodes.Status201Created, coupon);
    }

    // Burada rota kupon kimliğiyle gelen yönetici güncellemesini Application komutuna çeviriyorum.
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CouponDto>> Update(
        Guid id,
        CouponRequest request,
        CancellationToken cancellationToken)
    {
        var coupon = await _sender.Send(
            new UpdateCouponCommand(
                id,
                request.Code,
                request.DiscountType,
                request.DiscountValue,
                request.Description,
                request.MinimumOrderAmount,
                request.UsageLimit,
                request.StartsAt,
                request.ExpiresAt,
                request.IsMemberOnly),
            cancellationToken);
        return Ok(coupon);
    }

    // Burada yöneticinin kuponu yeni kullanımlara açma veya kapatma isteğini iletiyorum.
    [HttpPatch("{id:guid}/activation")]
    public async Task<ActionResult<CouponDto>> SetActivation(
        Guid id,
        SetCouponActivationRequest request,
        CancellationToken cancellationToken)
    {
        var coupon = await _sender.Send(
            new SetCouponActivationCommand(id, request.IsActive),
            cancellationToken);
        return Ok(coupon);
    }
}

// Burada kupon oluşturma ve güncelleme için istemciden kabul edilen yönetim alanlarını tanımlıyorum.
public sealed record CouponRequest(
    string Code,
    CouponDiscountType DiscountType,
    decimal DiscountValue,
    string? Description = null,
    decimal? MinimumOrderAmount = null,
    int? UsageLimit = null,
    DateTime? StartsAt = null,
    DateTime? ExpiresAt = null,
    bool IsActive = true,
    bool IsMemberOnly = false);

// Burada kupon aktiflik değişikliği için gereken tek HTTP alanını tanımlıyorum.
public sealed record SetCouponActivationRequest(bool IsActive);
