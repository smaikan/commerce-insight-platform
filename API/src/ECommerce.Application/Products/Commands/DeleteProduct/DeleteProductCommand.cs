using MediatR;

namespace ECommerce.Application.Products.Commands.DeleteProduct;

// Burada yönetici ürün silme isteğini Application katmanında temsil ediyorum.
public sealed record DeleteProductCommand(long Id) : IRequest;
