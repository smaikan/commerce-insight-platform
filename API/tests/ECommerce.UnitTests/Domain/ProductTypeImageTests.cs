using ECommerce.Application.ProductTypes.Commands.BulkCreateProductTypes;
using ECommerce.Application.ProductTypes.Commands.CreateProductType;
using ECommerce.Application.ProductTypes.Commands.UpdateProductType;
using ECommerce.Domain.Entities;
using FluentAssertions;

namespace ECommerce.UnitTests.Domain;

public sealed class ProductTypeImageTests
{
    // Burada ürün türü görselinin temizlenerek saklandığını ve boş değerle kaldırıldığını doğruluyorum.
    [Fact]
    public void ProductType_Should_Normalize_And_Clear_ImageUrl()
    {
        var productType = new ProductType(
            "Kategori",
            imageUrl: "  https://cdn.example.test/category.webp  ");

        productType.ImageUrl.Should().Be("https://cdn.example.test/category.webp");

        productType.SetImageUrl("  ");

        productType.ImageUrl.Should().BeNull();
    }

    // Burada tekli ve toplu ürün türü isteklerinin 500 karakterden uzun görselleri reddettiğini doğruluyorum.
    [Fact]
    public void Validators_Should_Reject_ImageUrl_Above_Maximum_Length()
    {
        var oversizedImageUrl = new string('x', ProductType.MaximumImageUrlLength + 1);

        var createResult = new CreateProductTypeCommandValidator().Validate(
            new CreateProductTypeCommand("Kategori", ImageUrl: oversizedImageUrl));
        var updateResult = new UpdateProductTypeCommandValidator().Validate(
            new UpdateProductTypeCommand(Guid.NewGuid(), "Kategori", ImageUrl: oversizedImageUrl));
        var bulkResult = new BulkCreateProductTypesCommandValidator().Validate(
            new BulkCreateProductTypesCommand(
                [new BulkCreateProductTypeItem("Kategori", ImageUrl: oversizedImageUrl)]));

        createResult.Errors.Should().Contain(error => error.PropertyName == "ImageUrl");
        updateResult.Errors.Should().Contain(error => error.PropertyName == "ImageUrl");
        bulkResult.Errors.Should().Contain(error => error.PropertyName == "ProductTypes[0].ImageUrl");
    }
}
