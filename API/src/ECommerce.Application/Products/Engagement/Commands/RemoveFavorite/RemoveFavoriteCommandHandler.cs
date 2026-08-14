using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Products.Engagement.Services;
using MediatR;

namespace ECommerce.Application.Products.Engagement.Commands.RemoveFavorite;

public sealed class RemoveFavoriteCommandHandler : IRequestHandler<RemoveFavoriteCommand>
{
    private readonly IProductRepository _products;
    private readonly IProductEngagementRepository _engagement;
    private readonly IFavoriteOwnerResolver _ownerResolver;
    private readonly IUnitOfWork _unitOfWork;

    // Burada favori silme için ürün, sahiplik ve kalıcılık bağımlılıklarını hazırlıyorum.
    public RemoveFavoriteCommandHandler(IProductRepository products, IProductEngagementRepository engagement,
        IFavoriteOwnerResolver ownerResolver, IUnitOfWork unitOfWork)
    {
        _products = products;
        _engagement = engagement;
        _ownerResolver = ownerResolver;
        _unitOfWork = unitOfWork;
    }

    // Burada kullanıcı veya guest session sahibinin favorisini sayaçlarla atomik biçimde kaldırıyorum.
    public async Task Handle(RemoveFavoriteCommand request, CancellationToken cancellationToken)
    {
        await _unitOfWork.ExecuteInSerializableTransactionAsync(
            transactionCancellationToken => RemoveFavoriteAsync(request, transactionCancellationToken),
            cancellationToken);
    }

    // Burada favori kaydı ile ürün sayacını aynı transaction içinde güncelliyorum.
    private async Task<bool> RemoveFavoriteAsync(
        RemoveFavoriteCommand request,
        CancellationToken cancellationToken)
    {
        var owner = _ownerResolver.Resolve(request.SessionId);
        // Burada claim akışıyla aynı kilit sırasını korumak için önce owner favori aralığını okuyorum.
        var favorite = await _engagement.GetFavoriteForUpdateAsync(request.ProductId, owner, cancellationToken)
            ?? throw new NotFoundException("Favorite was not found.");
        var product = await _products.GetByIdForUpdateAsync(request.ProductId, cancellationToken)
            ?? throw new NotFoundException("Product was not found.");
        _engagement.RemoveFavorite(favorite);
        product.DecreaseFavoriteCount();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}
