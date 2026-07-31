using FluentValidation;

namespace ECommerce.Application.Coupons.Queries.GetCoupons;

public sealed class GetCouponsQueryValidator : AbstractValidator<GetCouponsQuery>
{
    // Burada kupon listeleme sorgusunun sayfalama sÄ±nÄ±rlarÄ±nÄ± doÄŸruluyorum.
    public GetCouponsQueryValidator()
    {
        RuleFor(query => query.PageNumber)
            .InclusiveBetween(1, 10_000);

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, 100);
    }
}
