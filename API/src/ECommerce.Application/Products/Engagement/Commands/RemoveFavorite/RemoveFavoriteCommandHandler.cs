using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using MediatR;

namespace ECommerce.Application.Products.Engagement.Commands.RemoveFavorite;

public sealed class RemoveFavoriteCommandHandler : IRequestHandler<RemoveFavoriteCommand>
{
    private readonly IProductRepository _products;
    private readonly IProductEngagementRepository _engagement;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public RemoveFavoriteCommandHandler(IProductRepository products, IProductEngagementRepository engagement,
        ICurrentUserService currentUser, IUnitOfWork unitOfWork)
    {
        _products = products; _engagement = engagement; _currentUser = currentUser; _unitOfWork = unitOfWork;
    }

    public async Task Handle(RemoveFavoriteCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetRequiredUserId();
        var product = await _products.GetByIdForUpdateAsync(request.ProductId, cancellationToken)
            ?? throw new NotFoundException("Product was not found.");
        var favorite = await _engagement.GetFavoriteForUpdateAsync(product.Id, userId, cancellationToken)
            ?? throw new NotFoundException("Favorite was not found.");
        _engagement.RemoveFavorite(favorite);
        product.DecreaseFavoriteCount();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
