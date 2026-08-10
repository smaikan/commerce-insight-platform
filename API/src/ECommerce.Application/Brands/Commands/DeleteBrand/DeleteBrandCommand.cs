using MediatR;

namespace ECommerce.Application.Brands.Commands.DeleteBrand;

// Burada yönetici marka silme isteğini Application katmanında temsil ediyorum.
public sealed record DeleteBrandCommand(Guid Id) : IRequest;
