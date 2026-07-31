using ECommerce.Application.Accounting.Common.Calculations;
using ECommerce.Application.Accounting.CostLayers;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Application.Accounting;

public static class AccountingApplicationServiceRegistration
{
    // Burada ortak fatura hesaplama servislerini Accounting modül sınırında kaydediyorum.
    public static IServiceCollection AddAccountingApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IAccountingRoundingPolicy, AccountingRoundingPolicy>();
        services.AddScoped<IInvoiceCalculationService, InvoiceCalculationService>();
        services.AddScoped<
            IOpeningBalanceCostLayerWriter,
            OpeningBalanceCostLayerWriter>();
        return services;
    }
}
