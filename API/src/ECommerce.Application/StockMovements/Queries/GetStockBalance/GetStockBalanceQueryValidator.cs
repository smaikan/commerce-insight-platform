using FluentValidation;

namespace ECommerce.Application.StockMovements.Queries.GetStockBalance;

public sealed class GetStockBalanceQueryValidator : AbstractValidator<GetStockBalanceQuery>
{
    // Burada stok mutabakatı istenen varyant kimliğinin boş olmadığını doğruluyorum.
    public GetStockBalanceQueryValidator()
    {
        RuleFor(query => query.ProductVariantId).NotEmpty();
    }
}
