namespace ECommerce.Application.Orders.Dtos;

public sealed record OrderImportResultDto(OrderDto Order, bool WasImported);
