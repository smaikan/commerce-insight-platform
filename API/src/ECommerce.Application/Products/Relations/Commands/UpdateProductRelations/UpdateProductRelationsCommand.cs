using MediatR;

namespace ECommerce.Application.Products.Relations.Commands.UpdateProductRelations;

// Burada ürünün koleksiyon, etiket ve bundle ilişkilerini değiştirecek isteği taşıyorum.
public sealed record UpdateProductRelationsCommand(
    long ProductId,
    IReadOnlyList<string> Collections,
    IReadOnlyList<ProductBundleItemInput> BundleItems,
    IReadOnlyList<string>? Tags = null) : IRequest;

// Burada bundle içine alınacak ürün ve adet bilgisini taşıyorum.
public sealed record ProductBundleItemInput(long ProductId, int Quantity);
