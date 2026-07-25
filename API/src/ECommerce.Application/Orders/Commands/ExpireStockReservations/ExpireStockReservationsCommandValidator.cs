using FluentValidation;

namespace ECommerce.Application.Orders.Commands.ExpireStockReservations;

public sealed class ExpireStockReservationsCommandValidator : AbstractValidator<ExpireStockReservationsCommand>
{
    // Burada arka plan partisinin veritabanı kilitlerini sınırlı tutacak boyutunu doğruluyorum.
    public ExpireStockReservationsCommandValidator()
    {
        RuleFor(command => command.BatchSize).InclusiveBetween(1, 500);
    }
}
