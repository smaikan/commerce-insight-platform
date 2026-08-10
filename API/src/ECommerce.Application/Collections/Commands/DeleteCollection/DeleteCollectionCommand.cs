using MediatR;

namespace ECommerce.Application.Collections.Commands.DeleteCollection;

// Burada yönetici koleksiyon silme isteğini Application katmanında temsil ediyorum.
public sealed record DeleteCollectionCommand(Guid Id) : IRequest;
