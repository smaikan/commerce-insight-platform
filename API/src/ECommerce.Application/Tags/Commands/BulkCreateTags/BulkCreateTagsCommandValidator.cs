using FluentValidation;

namespace ECommerce.Application.Tags.Commands.BulkCreateTags;

public sealed class BulkCreateTagsCommandValidator : AbstractValidator<BulkCreateTagsCommand>
{
    public BulkCreateTagsCommandValidator()
    {
        RuleFor(command => command.Tags)
            .NotEmpty()
            .Must(tags => tags is not null && tags.Count <= 500)
            .WithMessage("A bulk tag request can contain at most 500 tags.");

        RuleForEach(command => command.Tags)
            .ChildRules(tag =>
            {
                tag.RuleFor(item => item.Name)
                    .NotEmpty()
                    .MaximumLength(150);

                tag.RuleFor(item => item.Url)
                    .MaximumLength(200);
            });
    }
}
