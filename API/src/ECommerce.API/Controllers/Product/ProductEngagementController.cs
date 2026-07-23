using ECommerce.API.Security;
using ECommerce.API.Routing;
using ECommerce.Application.Products.Engagement.Commands.AddFavorite;
using ECommerce.Application.Products.Engagement.Commands.CreateReview;
using ECommerce.Application.Products.Engagement.Commands.RecordProductActivity;
using ECommerce.Application.Products.Engagement.Commands.RemoveFavorite;
using ECommerce.Application.Products.Engagement.Commands.SetReviewApproval;
using ECommerce.Application.Products.Engagement.Commands.UpsertRating;
using ECommerce.Application.Products.Engagement.Queries.GetFavoriteProducts;
using ECommerce.Application.Products.Engagement.Queries.GetProductMetrics;
using ECommerce.Application.Products.Engagement.Queries.GetProductReviews;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers.Product;

[ApiController]
[Route("api/product-engagement")]
public sealed class ProductEngagementController : ControllerBase
{
    private readonly ISender _sender;

    // Burada ürün etkileşimi isteklerini Application katmanına iletecek göndericiyi hazırlıyorum.
    public ProductEngagementController(ISender sender) => _sender = sender;

    // Burada oturum açmış kullanıcının favori ürünlerini getiriyorum.
    [Authorize]
    [HttpGet("favorites")]
    public async Task<ActionResult> GetFavorites(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await _sender.Send(new GetFavoriteProductsQuery(pageNumber, pageSize), cancellationToken));

    // Burada oturum açmış kullanıcının ürünü favorilerine eklemesini sağlıyorum.
    [Authorize]
    [HttpPost("products/{productId}/favorites")]
    public async Task<IActionResult> AddFavorite(string productId, CancellationToken cancellationToken)
    {
        await _sender.Send(new AddFavoriteCommand(ApiPublicIdParser.ParseProductId(productId)), cancellationToken);
        return NoContent();
    }

    // Burada oturum açmış kullanıcının ürünü favorilerinden kaldırmasını sağlıyorum.
    [Authorize]
    [HttpDelete("products/{productId}/favorites")]
    public async Task<IActionResult> RemoveFavorite(string productId, CancellationToken cancellationToken)
    {
        await _sender.Send(new RemoveFavoriteCommand(ApiPublicIdParser.ParseProductId(productId)), cancellationToken);
        return NoContent();
    }

    // Burada teslim edilmiş ürüne kullanıcı puanı verilmesini sağlıyorum.
    [Authorize]
    [HttpPut("products/{productId}/rating")]
    public async Task<IActionResult> UpsertRating(
        string productId,
        RatingRequest request,
        CancellationToken cancellationToken)
    {
        await _sender.Send(new UpsertRatingCommand(ApiPublicIdParser.ParseProductId(productId), request.RatingValue), cancellationToken);
        return NoContent();
    }

    // Burada teslim edilmiş ürüne kullanıcı yorumu eklenmesini sağlıyorum.
    [Authorize]
    [HttpPost("products/{productId}/reviews")]
    public async Task<ActionResult> CreateReview(
        string productId,
        CreateReviewRequest request,
        CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await _sender.Send(new CreateReviewCommand(
            ApiPublicIdParser.ParseProductId(productId), request.Comment, request.Title, request.RatingValue), cancellationToken));

    // Burada ürünün onaylanmış yorumlarını anonim ziyaretçilere getiriyorum.
    [AllowAnonymous]
    [HttpGet("products/{productId}/reviews")]
    public async Task<ActionResult> GetReviews(
        string productId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await _sender.Send(new GetProductReviewsQuery(
            ApiPublicIdParser.ParseProductId(productId), pageNumber, pageSize, true), cancellationToken));

    // Burada adminin yorum onay durumunu değiştirmesini sağlıyorum.
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [HttpPatch("reviews/{reviewId:guid}/approval")]
    public async Task<IActionResult> SetReviewApproval(
        Guid reviewId,
        ReviewApprovalRequest request,
        CancellationToken cancellationToken)
    {
        await _sender.Send(new SetReviewApprovalCommand(reviewId, request.IsApproved), cancellationToken);
        return NoContent();
    }

    // Burada adminin ürün metriklerini tarih aralığıyla incelemesini sağlıyorum.
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [HttpGet("products/{productId}/metrics")]
    public async Task<ActionResult> GetMetrics(
        string productId,
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new GetProductMetricsQuery(ApiPublicIdParser.ParseProductId(productId), from, to), cancellationToken));

    // Burada herhangi bir oturum açmış müşterinin tıklama ve sepete ekleme hareketini kaydediyorum.
    [Authorize]
    [HttpPost("products/{productId}/activities")]
    public async Task<IActionResult> RecordActivity(
        string productId,
        RecordProductActivityRequest request,
        CancellationToken cancellationToken)
    {
        await _sender.Send(new RecordProductActivityCommand(
            ApiPublicIdParser.ParseProductId(productId), request.ActivityType, request.ProductVariantId, request.Quantity), cancellationToken);
        return NoContent();
    }
}

// Burada ürün puanlama isteğinin HTTP sözleşmesini tanımlıyorum.
public sealed record RatingRequest(int RatingValue);
// Burada ürün yorumu isteğinin HTTP sözleşmesini tanımlıyorum.
public sealed record CreateReviewRequest(string Comment, string? Title = null, int? RatingValue = null);
// Burada yorum onay isteğinin HTTP sözleşmesini tanımlıyorum.
public sealed record ReviewApprovalRequest(bool IsApproved);
// Burada müşteri ürün hareketinin HTTP sözleşmesini tanımlıyorum.
public sealed record RecordProductActivityRequest(
    ProductActivityType ActivityType,
    Guid? ProductVariantId = null,
    int Quantity = 1);
