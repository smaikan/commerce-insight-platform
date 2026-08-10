using FluentValidation;

namespace ECommerce.Application.Brands.Commands.BulkCreateBrands;

public sealed class BulkCreateBrandsCommandValidator : AbstractValidator<BulkCreateBrandsCommand>
{
    // Burada toplu marka isteğinin adet ve alan sınırlarını tanımlıyorum.
    public BulkCreateBrandsCommandValidator()
    {
        RuleFor(command => command.Brands)
            .NotEmpty()
            .Must(brands => brands is not null && brands.Count <= 500)
            .WithMessage("A bulk brand request can contain at most 500 brands.");

        RuleForEach(command => command.Brands)
            .ChildRules(brand =>
            {
                brand.RuleFor(item => item.Name)
                    .NotEmpty()
                    .MaximumLength(150);

                brand.RuleFor(item => item.Url)
                    .MaximumLength(200);

                brand.RuleFor(item => item.Description)
                    .MaximumLength(1000);

                brand.RuleFor(item => item.ImageUrl)
                    .MaximumLength(500);
            });
    }
}
