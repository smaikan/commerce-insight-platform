namespace ECommerce.Persistence.Migrations;

// Burada migration ve SQL Server doğrulamasının aynı tekrar çalıştırılabilir satış metriği backfill SQL'ini kullanmasını sağlıyorum.
public static class AuthoritativeSalesMetricBackfill
{
    public const string ProductSalesSql =
        """
        UPDATE item
        SET item.PaidSalesQuantity = item.Quantity
        FROM OrderItems AS item
        INNER JOIN Orders AS [order] ON [order].Id = item.OrderId
        WHERE [order].PaidAt IS NOT NULL
           OR EXISTS (
               SELECT 1
               FROM Payments AS payment
               WHERE payment.OrderId = [order].Id
                 AND payment.PaidAt IS NOT NULL);

        ;WITH ApprovedRefundQuantities AS
        (
            SELECT returnItem.OrderItemId, SUM(CONVERT(bigint, returnItem.Quantity)) AS Quantity
            FROM ReturnItems AS returnItem
            INNER JOIN ReturnRequests AS returnRequest
                ON returnRequest.Id = returnItem.ReturnRequestId
            WHERE returnRequest.Type = 'Refund'
              AND returnRequest.Status IN ('Approved', 'Completed')
            GROUP BY returnItem.OrderItemId
        )
        UPDATE item
        SET item.ReversedSalesQuantity =
            CASE
                WHEN
                    ([order].Status = 'Cancelled' AND
                     (EXISTS (
                          SELECT 1
                          FROM OrderCancellationOperations AS operation
                          WHERE operation.OrderId = [order].Id
                            AND operation.Status = 'Completed')
                      OR EXISTS (
                          SELECT 1
                          FROM Payments AS payment
                          WHERE payment.OrderId = [order].Id
                            AND payment.PaidAt IS NOT NULL
                            AND payment.Status IN ('Cancelled', 'Refunded'))))
                    OR
                    ([order].Status = 'Refunded' AND
                     NOT EXISTS (
                         SELECT 1
                         FROM ReturnRequests AS refundRequest
                         WHERE refundRequest.OrderId = [order].Id
                           AND refundRequest.Type = 'Refund'
                           AND refundRequest.Status IN ('Approved', 'Completed')))
                    THEN item.PaidSalesQuantity
                WHEN COALESCE(refund.Quantity, 0) >= item.PaidSalesQuantity
                    THEN item.PaidSalesQuantity
                ELSE CONVERT(int, COALESCE(refund.Quantity, 0))
            END
        FROM OrderItems AS item
        INNER JOIN Orders AS [order] ON [order].Id = item.OrderId
        LEFT JOIN ApprovedRefundQuantities AS refund ON refund.OrderItemId = item.Id;

        UPDATE product
        SET product.NetSalesQuantity = COALESCE(metric.Quantity, 0)
        FROM Products AS product
        LEFT JOIN
        (
            SELECT item.ProductId,
                   SUM(CONVERT(bigint, item.PaidSalesQuantity - item.ReversedSalesQuantity)) AS Quantity
            FROM OrderItems AS item
            GROUP BY item.ProductId
        ) AS metric ON metric.ProductId = product.Id;
        """;

    public const string ReturnIntentSql =
        """
        UPDATE returnItem
        SET returnItem.SalesMetricReversedQuantity = returnItem.Quantity
        FROM ReturnItems AS returnItem
        INNER JOIN ReturnRequests AS returnRequest
            ON returnRequest.Id = returnItem.ReturnRequestId
        WHERE returnRequest.Type = 'Refund'
          AND returnRequest.Status IN ('Approved', 'Completed');
        """;
}
