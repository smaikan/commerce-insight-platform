using MediatR;

namespace ECommerce.Application.ProductTypes.Commands.DeleteProductType;

// Burada yönetici ürün türü silme isteğini Application katmanında temsil ediyorum.
public sealed record DeleteProductTypeCommand(Guid Id) : IRequest;
