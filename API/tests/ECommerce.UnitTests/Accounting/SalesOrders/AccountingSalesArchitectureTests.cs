using System.Reflection;
using ECommerce.Domain.Accounting.SalesInvoices;
using ECommerce.Domain.Accounting.SalesOrders;
using ECommerce.Domain.Entities;
using FluentAssertions;

namespace ECommerce.UnitTests.Accounting.SalesOrders;

public sealed class AccountingSalesArchitectureTests
{
    // Burada muhasebe satış siparişinin kullanıcı, sepet, adres veya depo bağımlılığı taşımadığını kanıtlıyorum.
    [Fact]
    public void AccountingSalesOrder_Should_Not_Contain_Forbidden_Core_Identifiers()
    {
        var forbiddenNames = new[] { "UserId", "CartId", "AddressId", "WarehouseId" };

        var propertyNames = typeof(AccountingSalesOrder)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Select(property => property.Name);

        propertyNames.Should().NotIntersectWith(forbiddenNames);
    }

    // Burada Accounting satış aggregate'ının e-ticaret Order ve OrderItem tiplerini kullanmadığını kanıtlıyorum.
    [Fact]
    public void AccountingSales_Aggregate_Should_Not_Reference_ECommerce_Order_Types()
    {
        var referencedTypes = GetReferencedMemberTypes(typeof(AccountingSalesOrder))
            .Concat(GetReferencedMemberTypes(typeof(AccountingSalesOrderItem)))
            .Distinct()
            .ToArray();

        referencedTypes.Should().NotContain(typeof(Order));
        referencedTypes.Should().NotContain(typeof(OrderItem));
    }

    // Burada SalesInvoice domain API'sinde doğrudan StockMovement bağı veya oluşturma çağrısı olmadığını kanıtlıyorum.
    [Fact]
    public void SalesInvoice_Should_Not_Directly_Reference_Or_Create_StockMovement()
    {
        var referencedTypes = GetReferencedMemberTypes(typeof(SalesInvoice))
            .Concat(GetReferencedMemberTypes(typeof(SalesInvoiceLine)))
            .Distinct()
            .ToArray();
        var repositoryRoot = FindRepositoryRoot();
        var invoiceSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "src",
            "ECommerce.Domain",
            "Accounting",
            "SalesInvoices",
            "SalesInvoice.cs"));
        var lineSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "src",
            "ECommerce.Domain",
            "Accounting",
            "SalesInvoices",
            "SalesInvoiceLine.cs"));
        var combinedSource = invoiceSource + lineSource;

        referencedTypes.Should().NotContain(typeof(StockMovement));
        combinedSource.Should().NotContain("StockMovement");
        combinedSource.Should().NotContain("ApplyStockMovement");
        combinedSource.Should().NotContain("new StockMovement");
    }

    // Burada bir tipin alan, property, constructor ve metot imzalarında kullandığı tipleri topluyorum.
    private static IEnumerable<Type> GetReferencedMemberTypes(Type type)
    {
        const BindingFlags flags =
            BindingFlags.Instance |
            BindingFlags.Static |
            BindingFlags.Public |
            BindingFlags.NonPublic |
            BindingFlags.DeclaredOnly;
        var memberTypes = type.GetFields(flags)
            .Select(field => field.FieldType)
            .Concat(type.GetProperties(flags).Select(property => property.PropertyType))
            .Concat(type.GetConstructors(flags)
                .SelectMany(constructor => constructor.GetParameters())
                .Select(parameter => parameter.ParameterType))
            .Concat(type.GetMethods(flags)
                .SelectMany(method => method.GetParameters()
                    .Select(parameter => parameter.ParameterType)
                    .Append(method.ReturnType)));

        return memberTypes.SelectMany(ExpandType);
    }

    // Burada koleksiyon ve nullable gibi sarmalayıcı tiplerin içindeki gerçek tipleri de açıyorum.
    private static IEnumerable<Type> ExpandType(Type type)
    {
        yield return type;
        if (!type.IsGenericType)
        {
            yield break;
        }

        foreach (var argument in type.GetGenericArguments())
        {
            foreach (var nested in ExpandType(argument))
            {
                yield return nested;
            }
        }
    }

    // Burada test çalışma klasöründen çözüm kökünü güvenli biçimde buluyorum.
    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ECommerce.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("ECommerce solution root was not found.");
    }
}
