using ECommerce.Application.Accounting.CostLayers;
using ECommerce.Application.Accounting.Payments;
using ECommerce.Application.Accounting.Expenses;
using ECommerce.Application.Accounting.Cancellations;
using ECommerce.Application.Accounting.Reports;
using ECommerce.Application.Accounting.PurchaseInvoices;
using ECommerce.Application.Accounting.SalesOrders;
using ECommerce.Persistence.Accounting.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Persistence.Accounting;

public static class AccountingPersistenceServiceRegistration
{
    // Burada alış ve satış Accounting repository ile salt okunur adapter'larını modül sınırında kaydediyorum.
    public static IServiceCollection AddAccountingPersistenceServices(this IServiceCollection services)
    {
        services.AddScoped<IPurchaseInvoiceRepository, PurchaseInvoiceRepository>();
        services.AddScoped<IAccountingProductSnapshotReader, AccountingProductSnapshotReader>();
        services.AddScoped<IAccountingStockMovementReader, AccountingStockMovementReader>();
        services.AddScoped<IInventoryCostRepository, InventoryCostRepository>();
        services.AddScoped<
            IOpeningBalanceCostLayerRepository,
            OpeningBalanceCostLayerRepository>();
        services.AddScoped<
            IProductVariantCostHistoryReadRepository,
            ProductVariantCostHistoryRepository>();
        services.AddScoped<ICurrentAccountRepository, CurrentAccountRepository>();
        services.AddScoped<IAccountingSalesOrderRepository, AccountingSalesOrderRepository>();
        services.AddScoped<ISalesInvoiceRepository, SalesInvoiceRepository>();
        services.AddScoped<IAccountingSalesCatalogReader, AccountingSalesCatalogReader>();
        services.AddScoped<IAccountingSalesCostRepository, AccountingSalesCostRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IFinancialAccountRepository, FinancialAccountRepository>();
        services.AddScoped<IExpenseRepository, ExpenseRepository>();
        services.AddScoped<IAccountingCancellationRepository, AccountingCancellationRepository>();
        services.AddScoped<IAccountingReportReader, AccountingReportReader>();
        return services;
    }
}
