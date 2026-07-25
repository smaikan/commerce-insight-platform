using ECommerce.Application.Returns.Dtos;
using MediatR;

namespace ECommerce.Application.Returns.Commands.ReceiveReturnRequest;

// Burada yöneticinin onaylı iade veya değişim talebinin fiziksel ürünlerini teslim alma isteğini taşıyorum.
public sealed record ReceiveReturnRequestCommand(Guid ReturnRequestId) : IRequest<ReturnRequestDto>;
