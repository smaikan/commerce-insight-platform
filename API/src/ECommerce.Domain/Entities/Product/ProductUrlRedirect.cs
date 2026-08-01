using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public sealed class ProductUrlRedirect : AuditableEntity
{
    public long ProductId { get; private set; }
    public Product Product { get; private set; } = null!;
    public string Url { get; private set; } = null!;

    private ProductUrlRedirect()
    {
    }

    public ProductUrlRedirect(Product product, string url)
    {
        Product = product ?? throw new DomainException("Product cannot be empty.");
        ProductId = product.Id;

        if (string.IsNullOrWhiteSpace(url))
        {
            throw new DomainException("Product redirect url cannot be empty.");
        }

        Url = url.Trim();
    }
}
