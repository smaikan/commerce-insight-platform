using FluentValidation;

namespace ECommerce.Application.Collections.Commands.BulkCreateCollections;

public sealed class BulkCreateCollectionsCommandValidator : AbstractValidator<BulkCreateCollectionsCommand>
{
    public BulkCreateCollectionsCommandValidator()
    {
        RuleFor(command => command.Collections)
            .NotEmpty()
            .Must(collections => collections is not null && collections.Count <= 500)
            .WithMessage("A bulk collection request can contain at most 500 collections.");

        RuleForEach(command => command.Collections)
            .ChildRules(collection =>
            {
                collection.RuleFor(item => item.Name)
                    .NotEmpty()
                    .MaximumLength(150);

                collection.RuleFor(item => item.Url)
                    .MaximumLength(200);

                collection.RuleFor(item => item.Description)
                    .MaximumLength(1000);

                collection.RuleFor(item => item.DisplayOrder)
                    .GreaterThanOrEqualTo(0);
            });
    }
}
