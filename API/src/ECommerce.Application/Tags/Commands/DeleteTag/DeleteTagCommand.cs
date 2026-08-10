using MediatR;

namespace ECommerce.Application.Tags.Commands.DeleteTag;

// Burada yönetici etiket silme isteğini Application katmanında temsil ediyorum.
public sealed record DeleteTagCommand(Guid Id) : IRequest;
