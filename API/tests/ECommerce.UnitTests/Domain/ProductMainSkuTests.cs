using ECommerce.Domain.Common;
using ECommerce.Domain.Entities;
using FluentAssertions;

namespace ECommerce.UnitTests.Domain;

public sealed class ProductMainSkuTests
{
    // Burada ana SKU değerinin boşlukları temizlenerek büyük harfe dönüştürüldüğünü doğruluyorum.
    [Fact]
    public void Constructor_Should_Normalize_Main_Sku()
    {
        var product = new Product(
            "Product",
            "product",
            mainSku: "  product-main  ");

        product.MainSku.Should().Be("PRODUCT-MAIN");
    }

    // Burada boş ana SKU ile ürün oluşturulmasını Domain seviyesinde engelliyorum.
    [Fact]
    public void Constructor_Should_Reject_Empty_Main_Sku()
    {
        Action act = () => new Product(
            "Product",
            "product",
            mainSku: " ");

        act.Should().Throw<DomainException>();
    }

    // Burada ana SKU değerinin desteklenen uzunluğu aşmasını engelliyorum.
    [Fact]
    public void Constructor_Should_Reject_Too_Long_Main_Sku()
    {
        var mainSku = new string('A', Product.MaximumMainSkuLength + 1);

        Action act = () => new Product(
            "Product",
            "product",
            mainSku: mainSku);

        act.Should().Throw<DomainException>();
    }

    // Burada temel ürün güncellemesinin ana SKU ve concurrency değerini birlikte yenilediğini doğruluyorum.
    [Fact]
    public void UpdateBasics_Should_Change_Main_Sku_And_Concurrency_Token()
    {
        var product = new Product(
            "Product",
            "product",
            mainSku: "OLD-MAIN");
        var concurrencyToken = product.ConcurrencyToken;

        product.UpdateBasics(
            "Updated Product",
            "updated-product",
            description: null,
            displayOrder: 0,
            seoTitle: null,
            seoDescription: null,
            mainSku: " new-main ");

        product.MainSku.Should().Be("NEW-MAIN");
        product.ConcurrencyToken.Should().NotBe(concurrencyToken);
        product.UpdatedAt.Should().NotBeNull();
    }

    // Burada ürünün varyant durumunun gerçek varyant koleksiyonundan türetildiğini doğruluyorum.
    [Fact]
    public void HasVariants_Should_Reflect_Variant_Collection()
    {
        var product = new Product(
            "Product",
            "product",
            mainSku: "MAIN-SKU");

        product.HasVariants.Should().BeFalse();

        product.Variants.Add(new ProductVariant(
            product,
            "Default",
            "VARIANT-SKU",
            price: 10m,
            stock: 0));

        product.HasVariants.Should().BeTrue();
    }
}
