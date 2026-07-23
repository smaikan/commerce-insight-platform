using MediatR;

namespace ECommerce.Application.Products.Images.Commands.DeleteProductImage;

public sealed record DeleteProductImageCommand(Guid Id) : IRequest;
