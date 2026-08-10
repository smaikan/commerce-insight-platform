using System.Data;
using System.Data.Common;
using System.Globalization;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Dashboard;
using ECommerce.Application.Dashboard.Dtos;
using ECommerce.Domain.Enums;
using ECommerce.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ECommerce.Persistence.Repositories;

// Burada dashboard için yalnızca aggregate SQL sorgularını çalıştırıyorum.
public sealed class AdminDashboardReader : IAdminDashboardReader
{
    private readonly AppDbContext _context;
    private readonly int _lowStockThreshold;

    // Burada dashboard aggregate sorguları ve düşük stok eşiği için bağımlılıkları hazırlıyorum.
    public AdminDashboardReader(AppDbContext context, IOptions<DashboardOptions> options)
    {
        _context = context;
        _lowStockThreshold = options.Value.LowStockThreshold;
    }

    // Burada sipariş, gelir, ürün ve düşük stok metriklerini tek SQL round-trip'inde topluyorum.
    public async Task<DashboardOverviewDto> GetOverviewAsync(CancellationToken cancellationToken = default)
    {
        var connection = _context.Database.GetDbConnection();
        var shouldCloseConnection = connection.State != ConnectionState.Open;
        if (shouldCloseConnection)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT
                    (SELECT COUNT(*) FROM [Orders]) AS TotalOrderCount,
                    (SELECT COUNT(*) FROM [Orders] WHERE [Status] = @pendingStatus) AS PendingOrderCount,
                    (SELECT COUNT(*) FROM [Orders] WHERE [PaidAt] IS NOT NULL AND [Status] <> @refundedStatus) AS PaidOrderCount,
                    (SELECT COALESCE(SUM([GrandTotal]), 0) FROM [Orders] WHERE [PaidAt] IS NOT NULL AND [Status] <> @refundedStatus)
                    -
                    (SELECT COALESCE(SUM([ReturnRequests].[RefundTotal]), 0)
                     FROM [ReturnRequests]
                     INNER JOIN [Orders] AS [RefundOrders] ON [RefundOrders].[Id] = [ReturnRequests].[OrderId]
                     WHERE [ReturnRequests].[Type] = @refundType
                       AND [ReturnRequests].[Status] = @completedReturnStatus
                       AND [RefundOrders].[PaidAt] IS NOT NULL
                       AND [RefundOrders].[Status] <> @refundedStatus) AS PaidRevenue,
                    (SELECT COUNT(*) FROM [Products] WHERE [IsActive] = 1 AND [DeletedAtUtc] IS NULL) AS ActiveProductCount,
                    (SELECT COUNT(*)
                     FROM [ProductVariants]
                     INNER JOIN [Products] ON [Products].[Id] = [ProductVariants].[ProductId]
                     WHERE [ProductVariants].[IsActive] = 1
                       AND [ProductVariants].[Stock] > 0
                       AND [ProductVariants].[Stock] < @lowStockThreshold
                       AND [Products].[DeletedAtUtc] IS NULL) AS LowStockVariantCount;
                """;
            AddParameter(command, "@pendingStatus", OrderStatus.Pending.ToString());
            AddParameter(command, "@refundedStatus", OrderStatus.Refunded.ToString());
            AddParameter(command, "@refundType", ReturnType.Refund.ToString());
            AddParameter(command, "@completedReturnStatus", ReturnRequestStatus.Completed.ToString());
            AddParameter(command, "@lowStockThreshold", _lowStockThreshold);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                throw new InvalidOperationException("Dashboard aggregate query did not return a result.");
            }

            return new DashboardOverviewDto(
                ReadInt32(reader, 0),
                ReadInt32(reader, 1),
                ReadInt32(reader, 2),
                ReadDecimal(reader, 3),
                ReadInt32(reader, 4),
                ReadInt32(reader, 5),
                DateTime.UtcNow);
        }
        finally
        {
            if (shouldCloseConnection)
            {
                await connection.CloseAsync();
            }
        }
    }

    // Burada sağlayıcı bağımsız aggregate sayısını API sözleşmesindeki int değere dönüştürüyorum.
    private static int ReadInt32(DbDataReader reader, int ordinal) =>
        Convert.ToInt32(reader.GetValue(ordinal), CultureInfo.InvariantCulture);

    // Burada sağlayıcının döndürdüğü toplam değeri para hassasiyetinde okuyorum.
    private static decimal ReadDecimal(DbDataReader reader, int ordinal) =>
        Convert.ToDecimal(reader.GetValue(ordinal), CultureInfo.InvariantCulture);

    // Burada aggregate SQL sorgusuna sağlayıcı parametresi ekliyorum.
    private static void AddParameter(DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}
