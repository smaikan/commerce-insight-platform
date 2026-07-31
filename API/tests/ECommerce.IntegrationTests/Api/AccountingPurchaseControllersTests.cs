using System.Reflection;
using System.Text.Json;
using ECommerce.API.Security;
using ECommerce.API.Controllers.Accounting;
using ECommerce.Application.Accounting.CostLayers;
using ECommerce.Application.Accounting.PurchaseInvoices;
using ECommerce.Domain.Accounting.Common.Enums;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.IntegrationTests.Api;

public sealed class AccountingPurchaseControllersTests
{
    // Burada tek satır PUT payload'ının ürün ve snapshot kimlik alanlarını dış sözleşmeye açmadığını doğruluyorum.
    [Fact]
    public void Update_Line_Contract_Should_Contain_Only_Commercial_Fields()
    {
        var forbiddenNames = new[]
        {
            "LineNumber",
            "ProductId",
            "ProductVariantId",
            "ProductName",
            "VariantName",
            "Sku",
            "Barcode"
        };
        var propertyNames = typeof(PurchaseInvoiceLineCommercialUpdateInput)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(property => property.Name)
            .ToArray();
        var action = typeof(PurchaseInvoicesController)
            .GetMethod(nameof(PurchaseInvoicesController.UpdateLine))
            ?? throw new InvalidOperationException("Purchase line update endpoint was not found.");
        var lineParameter = action.GetParameters()
            .Single(parameter => parameter.Name == "line");

        propertyNames.Should().NotContain(name => forbiddenNames.Contains(name));
        lineParameter.ParameterType.Should().Be(typeof(PurchaseInvoiceLineCommercialUpdateInput));
    }

    // Burada HTTP JSON girdisi fiyat, para birimi ve kur alanlarını atladığında onaylı varsayılanların uygulandığını doğruluyorum.
    [Fact]
    public void Purchase_Json_Contracts_Should_Apply_Approved_Defaults()
    {
        var variantId = Guid.NewGuid();
        var currentAccountId = Guid.NewGuid();
        var line = JsonSerializer.Deserialize<PurchaseInvoiceLineInput>(
            $$"""
            {
              "LineNumber": 1,
              "ProductVariantId": "{{variantId}}",
              "PurchaseQuantity": 2,
              "UnitOfMeasure": "ADET",
              "UnitsPerPurchaseUnit": 1,
              "PriceEntryMode": {{(int)PriceEntryMode.ExcludingVat}},
              "VatRate": 20
            }
            """);
        var header = JsonSerializer.Deserialize<PurchaseInvoiceHeaderInput>(
            $$"""
            {
              "CurrentAccountId": "{{currentAccountId}}",
              "InvoiceNumber": "INV-DEFAULT",
              "InvoiceDate": "2026-07-26T00:00:00"
            }
            """);

        line.Should().NotBeNull();
        line!.EnteredUnitPrice.Should().Be(0m);
        header.Should().NotBeNull();
        header!.CurrencyCode.Should().Be("TRY");
        header.ExchangeRate.Should().Be(1m);
    }

    // Burada varyant maliyet geçmişi endpoint'inin doğru route, sorgu sözleşmesi ve AdminOnly korumasıyla yayımlandığını doğruluyorum.
    [Fact]
    public void Cost_History_Endpoint_Should_Be_AdminOnly_And_Variant_Based()
    {
        var controllerType = typeof(ProductVariantCostHistoryController);
        var route = controllerType
            .GetCustomAttribute<RouteAttribute>(inherit: true);
        var authorization = controllerType
            .GetCustomAttributes<AuthorizeAttribute>(inherit: true)
            .Single();
        var action = controllerType.GetMethod(
            nameof(ProductVariantCostHistoryController.Get));

        route.Should().NotBeNull();
        route!.Template.Should().Be(
            "api/accounting/product-variants/{productVariantId:guid}/cost-history");
        authorization.Policy.Should().Be(AuthorizationPolicies.AdminOnly);
        action.Should().NotBeNull();
        action!.GetCustomAttribute<HttpGetAttribute>().Should().NotBeNull();
        action.GetParameters()
            .Single(parameter => parameter.Name == "productVariantId")
            .ParameterType.Should().Be(typeof(Guid));
        typeof(GetProductVariantCostHistoryQuery)
            .GetProperty(nameof(GetProductVariantCostHistoryQuery.ProductVariantId))
            .Should()
            .NotBeNull();
    }

    // Burada OpeningBalance PATCH JSON'unda maliyetler atlandığında hariç maliyetin sıfır ve dahil maliyetin fallback için null kaldığını doğruluyorum.
    [Fact]
    public void Opening_Balance_Update_Json_Should_Apply_Optional_Cost_Defaults()
    {
        var concurrencyToken = Guid.NewGuid();

        var request = JsonSerializer.Deserialize<
            UpdateOpeningBalanceCostLayerRequest>(
            $$"""
            {
              "ExpectedConcurrencyToken": "{{concurrencyToken}}"
            }
            """);

        request.Should().NotBeNull();
        request!.ExpectedConcurrencyToken.Should().Be(concurrencyToken);
        request.UnitCostExcludingVat.Should().Be(0m);
        request.UnitCostIncludingVat.Should().BeNull();
    }
}
