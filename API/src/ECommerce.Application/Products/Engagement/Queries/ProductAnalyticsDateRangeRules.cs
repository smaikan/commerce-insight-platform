using FluentValidation;

namespace ECommerce.Application.Products.Engagement.Queries;

public static class ProductAnalyticsDateRangeRules
{
    public const int MaximumDayCount = 90;

    // Burada ürün analitiği sorgularına ortak ve en fazla doksan günlük tarih kuralını ekliyorum.
    public static void Apply<T>(
        AbstractValidator<T> validator,
        Func<T, DateOnly> from,
        Func<T, DateOnly> to)
    {
        validator.RuleFor(query => from(query))
            .LessThanOrEqualTo(query => to(query))
            .WithMessage("from tarihi to tarihinden sonra olamaz.");
        validator.RuleFor(query => to(query).DayNumber - from(query).DayNumber)
            .LessThan(MaximumDayCount)
            .When(query => from(query) <= to(query))
            .WithMessage("Tarih aralığı en fazla 90 gün olabilir.");
    }
}
