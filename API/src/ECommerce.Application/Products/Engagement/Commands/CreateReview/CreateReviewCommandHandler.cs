using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Products.Dtos;
using ECommerce.Domain.Entities;
using MediatR;

namespace ECommerce.Application.Products.Engagement.Commands.CreateReview;

public sealed class CreateReviewCommandHandler : IRequestHandler<CreateReviewCommand, ProductReviewDto>
{
    private readonly IProductRepository _products;
    private readonly IProductEngagementRepository _engagement;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    // Burada ürün yorumu işlemi için gerekli bağımlılıkları hazırlıyorum.
    public CreateReviewCommandHandler(IProductRepository products, IProductEngagementRepository engagement,
        ICurrentUserService currentUser, IUnitOfWork unitOfWork)
    {
        _products = products; _engagement = engagement; _currentUser = currentUser; _unitOfWork = unitOfWork;
    }

    // Burada yalnızca teslim edilmiş bir siparişte bulunan ürün için yorum oluşturuyorum.
    public async Task<ProductReviewDto> Handle(CreateReviewCommand request, CancellationToken cancellationToken)
    {
        if (await _products.GetByIdAsync(request.ProductId, cancellationToken) is null)
        {
            throw new NotFoundException("Product was not found.");
        }
        var userId = _currentUser.GetRequiredUserId();

        if (!await _engagement.HasDeliveredPurchaseAsync(request.ProductId, userId, cancellationToken))
        {
            throw new ConflictException("The product can only be reviewed after it has been delivered.");
        }

        var review = new ProductReview(request.ProductId, userId, request.Comment, request.Title, request.RatingValue);
        await _engagement.AddReviewAsync(review, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return review.ToDto();
    }
}
