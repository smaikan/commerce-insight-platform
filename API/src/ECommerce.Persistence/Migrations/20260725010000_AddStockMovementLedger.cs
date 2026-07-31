using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Persistence.Migrations;

public partial class AddStockMovementLedger : Migration
{
    // Burada eski stok işlem geçmişini kaybetmeden imzalı stok hareketi defterine dönüştürüyorum.
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_InventoryTransactions_Orders_OrderId",
            table: "InventoryTransactions");

        migrationBuilder.DropForeignKey(
            name: "FK_InventoryTransactions_ProductVariants_ProductVariantId",
            table: "InventoryTransactions");

        migrationBuilder.DropForeignKey(
            name: "FK_InventoryTransactions_ReturnRequests_ReturnRequestId",
            table: "InventoryTransactions");

        migrationBuilder.DropPrimaryKey(
            name: "PK_InventoryTransactions",
            table: "InventoryTransactions");

        migrationBuilder.DropIndex(
            name: "IX_InventoryTransactions_OrderId",
            table: "InventoryTransactions");

        migrationBuilder.DropIndex(
            name: "IX_InventoryTransactions_ProductVariantId",
            table: "InventoryTransactions");

        migrationBuilder.DropIndex(
            name: "IX_InventoryTransactions_ReturnRequestId",
            table: "InventoryTransactions");

        migrationBuilder.DropIndex(
            name: "IX_InventoryTransactions_ReturnRequestId_ProductVariantId",
            table: "InventoryTransactions");

        migrationBuilder.RenameTable(
            name: "InventoryTransactions",
            newName: "StockMovements");

        migrationBuilder.RenameColumn(
            name: "Quantity",
            table: "StockMovements",
            newName: "QuantityDelta");

        migrationBuilder.RenameColumn(
            name: "StockAfterTransaction",
            table: "StockMovements",
            newName: "StockAfterMovement");

        migrationBuilder.RenameColumn(
            name: "Type",
            table: "StockMovements",
            newName: "LegacyType");

        migrationBuilder.AddColumn<int>(
            name: "Direction",
            table: "StockMovements",
            type: "int",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "StockBeforeMovement",
            table: "StockMovements",
            type: "int",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "Type",
            table: "StockMovements",
            type: "int",
            nullable: true);

        migrationBuilder.Sql(
            """
            IF EXISTS
            (
                SELECT 1
                FROM [StockMovements]
                WHERE [QuantityDelta] <= 0 OR [StockAfterMovement] < 0
            )
                THROW 51000, 'Legacy inventory data contains a non-positive quantity or negative stock.', 1;

            IF EXISTS
            (
                SELECT 1
                FROM [ProductVariants]
                WHERE [Stock] < 0
            )
                THROW 51008, 'A product variant contains negative stock and cannot be reconciled.', 1;

            IF EXISTS
            (
                SELECT 1
                FROM [StockMovements]
                WHERE [LegacyType] IN (N'StockIn', N'OrderCancelled', N'Return')
                  AND [StockAfterMovement] < [QuantityDelta]
            )
                THROW 51001, 'Legacy incoming inventory data cannot produce a non-negative previous stock.', 1;

            IF EXISTS
            (
                SELECT 1
                FROM [StockMovements]
                WHERE [LegacyType] IN (N'StockOut', N'OrderCreated')
                  AND CONVERT(bigint, [StockAfterMovement]) + CONVERT(bigint, [QuantityDelta]) > 2147483647
            )
                THROW 51002, 'Legacy outgoing inventory data exceeds the supported stock range.', 1;

            IF EXISTS
            (
                SELECT 1
                FROM [StockMovements]
                WHERE [LegacyType] IN (N'OrderCreated', N'OrderCancelled')
                  AND [OrderId] IS NULL
            )
                THROW 51003, 'Legacy order inventory data is missing its order reference.', 1;

            IF EXISTS
            (
                SELECT 1
                FROM [StockMovements]
                WHERE [LegacyType] = N'Return'
                  AND [ReturnRequestId] IS NULL
            )
                THROW 51004, 'Legacy return inventory data is missing its return request reference.', 1;

            IF EXISTS
            (
                SELECT 1
                FROM
                (
                    SELECT
                        [LegacyType],
                        [QuantityDelta],
                        [StockAfterMovement],
                        LAG([StockAfterMovement]) OVER
                        (
                            PARTITION BY [ProductVariantId]
                            ORDER BY [CreatedAt], [Id]
                        ) AS [PreviousStock]
                    FROM [StockMovements]
                ) AS [ManualInput]
                WHERE [LegacyType] NOT IN
                    (N'StockIn', N'StockOut', N'OrderCreated', N'OrderCancelled', N'Return')
                  AND NOT
                  (
                      [StockAfterMovement] >= [QuantityDelta]
                      AND ([PreviousStock] IS NULL OR [StockAfterMovement] >= [PreviousStock])
                  )
                  AND CONVERT(bigint, [StockAfterMovement]) + CONVERT(bigint, [QuantityDelta]) > 2147483647
            )
                THROW 51005, 'Legacy manual inventory data exceeds the supported stock range.', 1;

            ;WITH [OrderedLegacy] AS
            (
                SELECT
                    [Id],
                    [LegacyType],
                    [QuantityDelta],
                    [StockAfterMovement],
                    ROW_NUMBER() OVER
                    (
                        PARTITION BY [ProductVariantId]
                        ORDER BY [CreatedAt], [Id]
                    ) AS [MovementNumber],
                    LAG([StockAfterMovement]) OVER
                    (
                        PARTITION BY [ProductVariantId]
                        ORDER BY [CreatedAt], [Id]
                    ) AS [PreviousStock]
                FROM [StockMovements]
            )
            UPDATE [Movement]
            SET
                [Type] = CASE
                    WHEN [OrderedLegacy].[LegacyType] = N'StockIn'
                         AND [OrderedLegacy].[MovementNumber] = 1 THEN 1
                    WHEN [OrderedLegacy].[LegacyType] IN (N'StockIn', N'StockOut', N'ManualAdjustment') THEN 30
                    WHEN [OrderedLegacy].[LegacyType] = N'OrderCreated' THEN 20
                    WHEN [OrderedLegacy].[LegacyType] = N'OrderCancelled' THEN 60
                    WHEN [OrderedLegacy].[LegacyType] = N'Return' THEN 21
                    ELSE 30
                END,
                [Direction] = CASE
                    WHEN [OrderedLegacy].[LegacyType] IN (N'StockIn', N'OrderCancelled', N'Return') THEN 1
                    WHEN [OrderedLegacy].[LegacyType] IN (N'StockOut', N'OrderCreated') THEN 2
                    WHEN [OrderedLegacy].[StockAfterMovement] >= [OrderedLegacy].[QuantityDelta]
                         AND
                         (
                             [OrderedLegacy].[PreviousStock] IS NULL
                             OR [OrderedLegacy].[StockAfterMovement] >= [OrderedLegacy].[PreviousStock]
                         )
                        THEN 1
                    ELSE 2
                END,
                [QuantityDelta] = CASE
                    WHEN [OrderedLegacy].[LegacyType] IN (N'StockIn', N'OrderCancelled', N'Return')
                        THEN [OrderedLegacy].[QuantityDelta]
                    WHEN [OrderedLegacy].[LegacyType] IN (N'StockOut', N'OrderCreated')
                        THEN -[OrderedLegacy].[QuantityDelta]
                    WHEN [OrderedLegacy].[StockAfterMovement] >= [OrderedLegacy].[QuantityDelta]
                         AND
                         (
                             [OrderedLegacy].[PreviousStock] IS NULL
                             OR [OrderedLegacy].[StockAfterMovement] >= [OrderedLegacy].[PreviousStock]
                         )
                        THEN [OrderedLegacy].[QuantityDelta]
                    ELSE -[OrderedLegacy].[QuantityDelta]
                END,
                [StockBeforeMovement] = CASE
                    WHEN [OrderedLegacy].[LegacyType] IN (N'StockIn', N'OrderCancelled', N'Return')
                        THEN [OrderedLegacy].[StockAfterMovement] - [OrderedLegacy].[QuantityDelta]
                    WHEN [OrderedLegacy].[LegacyType] IN (N'StockOut', N'OrderCreated')
                        THEN [OrderedLegacy].[StockAfterMovement] + [OrderedLegacy].[QuantityDelta]
                    WHEN [OrderedLegacy].[StockAfterMovement] >= [OrderedLegacy].[QuantityDelta]
                         AND
                         (
                             [OrderedLegacy].[PreviousStock] IS NULL
                             OR [OrderedLegacy].[StockAfterMovement] >= [OrderedLegacy].[PreviousStock]
                         )
                        THEN [OrderedLegacy].[StockAfterMovement] - [OrderedLegacy].[QuantityDelta]
                    ELSE [OrderedLegacy].[StockAfterMovement] + [OrderedLegacy].[QuantityDelta]
                END,
                [Reason] = CASE
                    WHEN [OrderedLegacy].[LegacyType] IN
                        (N'StockIn', N'StockOut', N'OrderCreated', N'OrderCancelled', N'ManualAdjustment', N'Return')
                        THEN [Movement].[Reason]
                    ELSE LEFT(
                        CONCAT(
                            CASE
                                WHEN NULLIF(LTRIM(RTRIM([Movement].[Reason])), N'') IS NULL THEN N''
                                ELSE CONCAT([Movement].[Reason], N' | ')
                            END,
                            N'Legacy type: ',
                            [OrderedLegacy].[LegacyType]),
                        500)
                END
            FROM [StockMovements] AS [Movement]
            INNER JOIN [OrderedLegacy] ON [OrderedLegacy].[Id] = [Movement].[Id];

            ;WITH [DuplicateOrderMovements] AS
            (
                SELECT
                    [Id],
                    ROW_NUMBER() OVER
                    (
                        PARTITION BY [OrderId], [ProductVariantId], [Type]
                        ORDER BY [CreatedAt], [Id]
                    ) AS [DuplicateNumber]
                FROM [StockMovements]
                WHERE [OrderId] IS NOT NULL
                  AND [ReturnRequestId] IS NULL
                  AND [Type] IN (20, 60)
            )
            UPDATE [Movement]
            SET
                [Type] = 30,
                [Reason] = LEFT(
                    CONCAT(
                        CASE
                            WHEN NULLIF(LTRIM(RTRIM([Movement].[Reason])), N'') IS NULL THEN N''
                            ELSE CONCAT([Movement].[Reason], N' | ')
                        END,
                        N'Legacy duplicate order movement'),
                    500)
            FROM [StockMovements] AS [Movement]
            INNER JOIN [DuplicateOrderMovements]
                ON [DuplicateOrderMovements].[Id] = [Movement].[Id]
            WHERE [DuplicateOrderMovements].[DuplicateNumber] > 1;

            ;WITH [DuplicateReturnMovements] AS
            (
                SELECT
                    [Id],
                    ROW_NUMBER() OVER
                    (
                        PARTITION BY [ReturnRequestId], [ProductVariantId], [Type]
                        ORDER BY [CreatedAt], [Id]
                    ) AS [DuplicateNumber]
                FROM [StockMovements]
                WHERE [ReturnRequestId] IS NOT NULL AND [Type] IN (20, 21)
            )
            UPDATE [Movement]
            SET
                [Type] = 30,
                [Reason] = LEFT(
                    CONCAT(
                        CASE
                            WHEN NULLIF(LTRIM(RTRIM([Movement].[Reason])), N'') IS NULL THEN N''
                            ELSE CONCAT([Movement].[Reason], N' | ')
                        END,
                        N'Legacy duplicate return movement'),
                    500)
            FROM [StockMovements] AS [Movement]
            INNER JOIN [DuplicateReturnMovements]
                ON [DuplicateReturnMovements].[Id] = [Movement].[Id]
            WHERE [DuplicateReturnMovements].[DuplicateNumber] > 1;

            INSERT INTO [StockMovements]
            (
                [Id],
                [ProductVariantId],
                [Direction],
                [Type],
                [QuantityDelta],
                [StockBeforeMovement],
                [StockAfterMovement],
                [Reason],
                [OrderId],
                [ReturnRequestId],
                [CreatedAt],
                [LegacyType]
            )
            SELECT
                CONVERT(
                    uniqueidentifier,
                    SUBSTRING(
                        HASHBYTES(
                            'SHA2_256',
                            CONCAT(
                                N'stock-ledger-opening-current:',
                                CONVERT(nvarchar(36), [Variant].[Id]))),
                        1,
                        16)),
                [Variant].[Id],
                1,
                1,
                [Variant].[Stock],
                0,
                [Variant].[Stock],
                N'Migration opening balance reconstructed from current variant stock.',
                NULL,
                NULL,
                [Variant].[CreatedAt],
                N'StockIn'
            FROM [ProductVariants] AS [Variant]
            WHERE [Variant].[Stock] > 0
              AND NOT EXISTS
              (
                  SELECT 1
                  FROM [StockMovements] AS [Movement]
                  WHERE [Movement].[ProductVariantId] = [Variant].[Id]
              );

            IF EXISTS
            (
                SELECT 1
                FROM
                (
                    SELECT
                        [ProductVariantId],
                        SUM(CONVERT(bigint, [QuantityDelta])) AS [LedgerTotal]
                    FROM [StockMovements]
                    GROUP BY [ProductVariantId]
                ) AS [Ledger]
                WHERE [Ledger].[LedgerTotal] < -2147483647
                   OR [Ledger].[LedgerTotal] > 2147483647
            )
                THROW 51006, 'Legacy inventory ledger requires a correction outside the supported stock range.', 1;

            ;WITH [NegativeLedgers] AS
            (
                SELECT
                    [Variant].[Id] AS [ProductVariantId],
                    SUM(CONVERT(bigint, [Movement].[QuantityDelta])) AS [LedgerTotal],
                    MIN([Movement].[CreatedAt]) AS [FirstMovementAt]
                FROM [ProductVariants] AS [Variant]
                INNER JOIN [StockMovements] AS [Movement]
                    ON [Movement].[ProductVariantId] = [Variant].[Id]
                GROUP BY [Variant].[Id]
                HAVING SUM(CONVERT(bigint, [Movement].[QuantityDelta])) < 0
            )
            INSERT INTO [StockMovements]
            (
                [Id],
                [ProductVariantId],
                [Direction],
                [Type],
                [QuantityDelta],
                [StockBeforeMovement],
                [StockAfterMovement],
                [Reason],
                [OrderId],
                [ReturnRequestId],
                [CreatedAt],
                [LegacyType]
            )
            SELECT
                CONVERT(
                    uniqueidentifier,
                    SUBSTRING(
                        HASHBYTES(
                            'SHA2_256',
                            CONCAT(
                                N'stock-ledger-opening-negative:',
                                CONVERT(nvarchar(36), [NegativeLedgers].[ProductVariantId]))),
                        1,
                        16)),
                [NegativeLedgers].[ProductVariantId],
                1,
                1,
                CONVERT(int, -[NegativeLedgers].[LedgerTotal]),
                0,
                CONVERT(int, -[NegativeLedgers].[LedgerTotal]),
                N'Migration opening balance added to normalize a negative legacy ledger.',
                NULL,
                NULL,
                CASE
                    WHEN [NegativeLedgers].[FirstMovementAt] >
                         CONVERT(datetime2, N'0001-01-01T00:00:00.001')
                        THEN DATEADD(millisecond, -1, [NegativeLedgers].[FirstMovementAt])
                    ELSE [NegativeLedgers].[FirstMovementAt]
                END,
                N'StockIn'
            FROM [NegativeLedgers];

            ;WITH [LedgerState] AS
            (
                SELECT
                    [Variant].[Id] AS [ProductVariantId],
                    [Variant].[Stock] AS [CurrentStock],
                    COALESCE(SUM(CONVERT(bigint, [Movement].[QuantityDelta])), 0) AS [LedgerTotal],
                    MAX([Movement].[CreatedAt]) AS [LastMovementAt]
                FROM [ProductVariants] AS [Variant]
                LEFT JOIN [StockMovements] AS [Movement]
                    ON [Movement].[ProductVariantId] = [Variant].[Id]
                GROUP BY [Variant].[Id], [Variant].[Stock]
            )
            INSERT INTO [StockMovements]
            (
                [Id],
                [ProductVariantId],
                [Direction],
                [Type],
                [QuantityDelta],
                [StockBeforeMovement],
                [StockAfterMovement],
                [Reason],
                [OrderId],
                [ReturnRequestId],
                [CreatedAt],
                [LegacyType]
            )
            SELECT
                CONVERT(
                    uniqueidentifier,
                    SUBSTRING(
                        HASHBYTES(
                            'SHA2_256',
                            CONCAT(
                                N'stock-ledger-stock-count:',
                                CONVERT(nvarchar(36), [LedgerState].[ProductVariantId]))),
                        1,
                        16)),
                [LedgerState].[ProductVariantId],
                CASE WHEN [LedgerState].[CurrentStock] > [LedgerState].[LedgerTotal] THEN 1 ELSE 2 END,
                31,
                CONVERT(int, CONVERT(bigint, [LedgerState].[CurrentStock]) - [LedgerState].[LedgerTotal]),
                CONVERT(int, [LedgerState].[LedgerTotal]),
                [LedgerState].[CurrentStock],
                N'Migration stock count adjustment aligned the ledger with current variant stock.',
                NULL,
                NULL,
                COALESCE(
                    CASE
                        WHEN [LedgerState].[LastMovementAt] <
                             CONVERT(datetime2, N'9999-12-30T23:59:59.9999999')
                            THEN DATEADD(millisecond, 1, [LedgerState].[LastMovementAt])
                        ELSE [LedgerState].[LastMovementAt]
                    END,
                    SYSUTCDATETIME()),
                N'ManualAdjustment'
            FROM [LedgerState]
            WHERE [LedgerState].[LedgerTotal] <> [LedgerState].[CurrentStock];

            IF EXISTS
            (
                SELECT 1
                FROM [ProductVariants] AS [Variant]
                OUTER APPLY
                (
                    SELECT COALESCE(SUM(CONVERT(bigint, [Movement].[QuantityDelta])), 0) AS [LedgerTotal]
                    FROM [StockMovements] AS [Movement]
                    WHERE [Movement].[ProductVariantId] = [Variant].[Id]
                ) AS [Ledger]
                WHERE [Ledger].[LedgerTotal] <> [Variant].[Stock]
            )
                THROW 51007, 'Stock movement ledger could not be reconciled with current variant stock.', 1;
            """);

        migrationBuilder.DropColumn(
            name: "LegacyType",
            table: "StockMovements");

        migrationBuilder.AlterColumn<int>(
            name: "Direction",
            table: "StockMovements",
            type: "int",
            nullable: false,
            oldClrType: typeof(int),
            oldType: "int",
            oldNullable: true);

        migrationBuilder.AlterColumn<int>(
            name: "StockBeforeMovement",
            table: "StockMovements",
            type: "int",
            nullable: false,
            oldClrType: typeof(int),
            oldType: "int",
            oldNullable: true);

        migrationBuilder.AlterColumn<int>(
            name: "Type",
            table: "StockMovements",
            type: "int",
            nullable: false,
            oldClrType: typeof(int),
            oldType: "int",
            oldNullable: true);

        migrationBuilder.AddPrimaryKey(
            name: "PK_StockMovements",
            table: "StockMovements",
            column: "Id");

        migrationBuilder.AddCheckConstraint(
            name: "CK_StockMovements_Direction_Matches_Delta",
            table: "StockMovements",
            sql: "([Direction] = 1 AND [QuantityDelta] > 0) OR ([Direction] = 2 AND [QuantityDelta] < 0)");

        migrationBuilder.AddCheckConstraint(
            name: "CK_StockMovements_QuantityDelta_NonZero",
            table: "StockMovements",
            sql: "[QuantityDelta] <> 0");

        migrationBuilder.AddCheckConstraint(
            name: "CK_StockMovements_Required_Reference",
            table: "StockMovements",
            sql: "([Type] NOT IN (20, 60) OR [OrderId] IS NOT NULL) AND ([Type] <> 21 OR [ReturnRequestId] IS NOT NULL)");

        migrationBuilder.AddCheckConstraint(
            name: "CK_StockMovements_Stock_Equation",
            table: "StockMovements",
            sql: "CAST([StockAfterMovement] AS bigint) = CAST([StockBeforeMovement] AS bigint) + CAST([QuantityDelta] AS bigint)");

        migrationBuilder.AddCheckConstraint(
            name: "CK_StockMovements_Stock_NonNegative",
            table: "StockMovements",
            sql: "[StockBeforeMovement] >= 0 AND [StockAfterMovement] >= 0");

        migrationBuilder.AddCheckConstraint(
            name: "CK_StockMovements_Type_Matches_Direction",
            table: "StockMovements",
            sql: "([Type] IN (1, 10, 21, 50, 60) AND [Direction] = 1) OR ([Type] IN (11, 20, 40, 41, 42, 51) AND [Direction] = 2) OR [Type] IN (30, 31)");

        migrationBuilder.AddCheckConstraint(
            name: "CK_StockMovements_Type_Valid",
            table: "StockMovements",
            sql: "[Type] IN (1, 10, 11, 20, 21, 30, 31, 40, 41, 42, 50, 51, 60)");

        migrationBuilder.CreateIndex(
            name: "IX_StockMovements_CreatedAt_Id",
            table: "StockMovements",
            columns: new[] { "CreatedAt", "Id" });

        migrationBuilder.CreateIndex(
            name: "IX_StockMovements_OrderId",
            table: "StockMovements",
            column: "OrderId");

        migrationBuilder.CreateIndex(
            name: "IX_StockMovements_ProductVariantId_CreatedAt",
            table: "StockMovements",
            columns: new[] { "ProductVariantId", "CreatedAt" });

        migrationBuilder.CreateIndex(
            name: "IX_StockMovements_ReturnRequestId",
            table: "StockMovements",
            column: "ReturnRequestId");

        migrationBuilder.CreateIndex(
            name: "UX_StockMovements_OrderId_ProductVariantId_Type",
            table: "StockMovements",
            columns: new[] { "OrderId", "ProductVariantId", "Type" },
            unique: true,
            filter: "[OrderId] IS NOT NULL AND [ReturnRequestId] IS NULL AND [Type] IN (20, 60)");

        migrationBuilder.CreateIndex(
            name: "UX_StockMovements_ReturnRequestId_ProductVariantId_Type",
            table: "StockMovements",
            columns: new[] { "ReturnRequestId", "ProductVariantId", "Type" },
            unique: true,
            filter: "[ReturnRequestId] IS NOT NULL AND [Type] IN (20, 21)");

        migrationBuilder.AddForeignKey(
            name: "FK_StockMovements_Orders_OrderId",
            table: "StockMovements",
            column: "OrderId",
            principalTable: "Orders",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_StockMovements_ProductVariants_ProductVariantId",
            table: "StockMovements",
            column: "ProductVariantId",
            principalTable: "ProductVariants",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_StockMovements_ReturnRequests_ReturnRequestId",
            table: "StockMovements",
            column: "ReturnRequestId",
            principalTable: "ReturnRequests",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

    // Burada yeni hareket kayıtlarını koruyarak şemayı eski stok işlem biçimine geri dönüştürüyorum.
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_StockMovements_Orders_OrderId",
            table: "StockMovements");

        migrationBuilder.DropForeignKey(
            name: "FK_StockMovements_ProductVariants_ProductVariantId",
            table: "StockMovements");

        migrationBuilder.DropForeignKey(
            name: "FK_StockMovements_ReturnRequests_ReturnRequestId",
            table: "StockMovements");

        migrationBuilder.DropPrimaryKey(
            name: "PK_StockMovements",
            table: "StockMovements");

        migrationBuilder.DropCheckConstraint(
            name: "CK_StockMovements_Direction_Matches_Delta",
            table: "StockMovements");

        migrationBuilder.DropCheckConstraint(
            name: "CK_StockMovements_QuantityDelta_NonZero",
            table: "StockMovements");

        migrationBuilder.DropCheckConstraint(
            name: "CK_StockMovements_Required_Reference",
            table: "StockMovements");

        migrationBuilder.DropCheckConstraint(
            name: "CK_StockMovements_Stock_Equation",
            table: "StockMovements");

        migrationBuilder.DropCheckConstraint(
            name: "CK_StockMovements_Stock_NonNegative",
            table: "StockMovements");

        migrationBuilder.DropCheckConstraint(
            name: "CK_StockMovements_Type_Matches_Direction",
            table: "StockMovements");

        migrationBuilder.DropCheckConstraint(
            name: "CK_StockMovements_Type_Valid",
            table: "StockMovements");

        migrationBuilder.DropIndex(
            name: "IX_StockMovements_CreatedAt_Id",
            table: "StockMovements");

        migrationBuilder.DropIndex(
            name: "IX_StockMovements_OrderId",
            table: "StockMovements");

        migrationBuilder.DropIndex(
            name: "IX_StockMovements_ProductVariantId_CreatedAt",
            table: "StockMovements");

        migrationBuilder.DropIndex(
            name: "IX_StockMovements_ReturnRequestId",
            table: "StockMovements");

        migrationBuilder.DropIndex(
            name: "UX_StockMovements_OrderId_ProductVariantId_Type",
            table: "StockMovements");

        migrationBuilder.DropIndex(
            name: "UX_StockMovements_ReturnRequestId_ProductVariantId_Type",
            table: "StockMovements");

        migrationBuilder.RenameColumn(
            name: "Type",
            table: "StockMovements",
            newName: "MovementTypeValue");

        migrationBuilder.AddColumn<string>(
            name: "Type",
            table: "StockMovements",
            type: "nvarchar(40)",
            maxLength: 40,
            nullable: true);

        migrationBuilder.Sql(
            """
            UPDATE [StockMovements]
            SET
                [Type] = CASE
                    WHEN [MovementTypeValue] IN (1, 10, 50) THEN N'StockIn'
                    WHEN [MovementTypeValue] IN (11, 40, 41, 42, 51) THEN N'StockOut'
                    WHEN [MovementTypeValue] = 20 AND [OrderId] IS NOT NULL THEN N'OrderCreated'
                    WHEN [MovementTypeValue] = 20 THEN N'StockOut'
                    WHEN [MovementTypeValue] = 21 AND [ReturnRequestId] IS NOT NULL THEN N'Return'
                    WHEN [MovementTypeValue] = 21 THEN N'StockIn'
                    WHEN [MovementTypeValue] IN (30, 31) THEN N'ManualAdjustment'
                    WHEN [MovementTypeValue] = 60 AND [OrderId] IS NOT NULL THEN N'OrderCancelled'
                    WHEN [MovementTypeValue] = 60 THEN N'StockIn'
                    ELSE N'ManualAdjustment'
                END,
                [QuantityDelta] = ABS([QuantityDelta]);
            """);

        migrationBuilder.DropColumn(
            name: "MovementTypeValue",
            table: "StockMovements");

        migrationBuilder.DropColumn(
            name: "Direction",
            table: "StockMovements");

        migrationBuilder.DropColumn(
            name: "StockBeforeMovement",
            table: "StockMovements");

        migrationBuilder.AlterColumn<string>(
            name: "Type",
            table: "StockMovements",
            type: "nvarchar(40)",
            maxLength: 40,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "nvarchar(40)",
            oldMaxLength: 40,
            oldNullable: true);

        migrationBuilder.RenameColumn(
            name: "QuantityDelta",
            table: "StockMovements",
            newName: "Quantity");

        migrationBuilder.RenameColumn(
            name: "StockAfterMovement",
            table: "StockMovements",
            newName: "StockAfterTransaction");

        migrationBuilder.RenameTable(
            name: "StockMovements",
            newName: "InventoryTransactions");

        migrationBuilder.AddPrimaryKey(
            name: "PK_InventoryTransactions",
            table: "InventoryTransactions",
            column: "Id");

        migrationBuilder.CreateIndex(
            name: "IX_InventoryTransactions_OrderId",
            table: "InventoryTransactions",
            column: "OrderId");

        migrationBuilder.CreateIndex(
            name: "IX_InventoryTransactions_ProductVariantId",
            table: "InventoryTransactions",
            column: "ProductVariantId");

        migrationBuilder.CreateIndex(
            name: "IX_InventoryTransactions_ReturnRequestId",
            table: "InventoryTransactions",
            column: "ReturnRequestId");

        migrationBuilder.CreateIndex(
            name: "IX_InventoryTransactions_ReturnRequestId_ProductVariantId",
            table: "InventoryTransactions",
            columns: new[] { "ReturnRequestId", "ProductVariantId" },
            unique: true,
            filter: "[ReturnRequestId] IS NOT NULL AND [Type] = 'Return'");

        migrationBuilder.AddForeignKey(
            name: "FK_InventoryTransactions_Orders_OrderId",
            table: "InventoryTransactions",
            column: "OrderId",
            principalTable: "Orders",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_InventoryTransactions_ProductVariants_ProductVariantId",
            table: "InventoryTransactions",
            column: "ProductVariantId",
            principalTable: "ProductVariants",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "FK_InventoryTransactions_ReturnRequests_ReturnRequestId",
            table: "InventoryTransactions",
            column: "ReturnRequestId",
            principalTable: "ReturnRequests",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }
}
