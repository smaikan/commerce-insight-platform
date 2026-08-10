using FluentValidation;

namespace ECommerce.Application.StorefrontBanners.Commands.UpdateStorefrontBanners;

public sealed class UpdateStorefrontBannersCommandValidator : AbstractValidator<UpdateStorefrontBannersCommand>
{
    // Burada ana banner ile en fazla beş alt banner URL sınırlarını tanımlıyorum.
    public UpdateStorefrontBannersCommandValidator()
    {
        RuleFor(command => command.MainBannerImageUrl)
            .MaximumLength(500);

        RuleFor(command => command.AltBannerImageUrls)
            .Must(urls => urls is null || urls.Count <= 5)
            .WithMessage("At most five alternate banner image urls can be supplied.");

        RuleForEach(command => command.AltBannerImageUrls)
            .NotEmpty()
            .MaximumLength(500);
    }
}
