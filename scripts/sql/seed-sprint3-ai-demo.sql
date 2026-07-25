SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @RequestedByUserId int =
    (
        SELECT TOP (1) Id
        FROM dbo.Users_Domain
        WHERE IsDeleted = 0
        ORDER BY Id
    );

    IF @RequestedByUserId IS NULL
        THROW 51001, 'Seed requires at least one active Users_Domain row.', 1;

    DECLARE @VendorTaxNumber nvarchar(256) = N'314455166300003';
    DECLARE @VendorId int;

    SELECT @VendorId = Id
    FROM dbo.Vendors
    WHERE TaxRegistrationNumber = @VendorTaxNumber
      AND IsDeleted = 0;

    IF @VendorId IS NULL
    BEGIN
        INSERT INTO dbo.Vendors
        (
            ErpId,
            Name,
            TaxRegistrationNumber,
            IsApproved,
            CreatedAt,
            IsDeleted
        )
        VALUES
        (
            N'SPRINT3-AI-DEMO',
            N'شركة تكنولوجيا السرعة - تكنولوجيا السرعة',
            @VendorTaxNumber,
            1,
            SYSUTCDATETIME(),
            0
        );

        SET @VendorId = CONVERT(int, SCOPE_IDENTITY());
    END;
    ELSE
    BEGIN
        UPDATE dbo.Vendors
        SET Name = N'شركة تكنولوجيا السرعة - تكنولوجيا السرعة',
            IsApproved = 1,
            UpdatedAt = SYSUTCDATETIME()
        WHERE Id = @VendorId;
    END;

    DECLARE @Items table
    (
        SupplierSku nvarchar(256) NOT NULL PRIMARY KEY,
        ProductName nvarchar(256) NOT NULL,
        Quantity int NOT NULL,
        UnitPrice decimal(18, 2) NOT NULL,
        LineTotal decimal(18, 2) NOT NULL,
        VatPercentage decimal(18, 2) NOT NULL,
        VatAmount decimal(18, 2) NOT NULL,
        Amount decimal(18, 2) NOT NULL
    );

    INSERT INTO @Items
    (
        SupplierSku,
        ProductName,
        Quantity,
        UnitPrice,
        LineTotal,
        VatPercentage,
        VatAmount,
        Amount
    )
    VALUES
        (N'10711', N'معمول اصابع مغطي بالشوكولاتة و المكسرات * 12', 5,   11.00, 55.00,   15.00, 8.25,   63.25),
        (N'10712', N'معمول اصابع مغطي بالشوكولاتة 1 * 12',           6,   11.00, 66.00,   15.00, 9.90,   75.90),
        (N'10713', N'معمول اصابع مغطي بالشوكولاتة البيضاء 1 * 12', 60,   11.00, 660.00,  15.00, 99.00,  759.00),
        (N'10716', N'معمول اسطمبة تمر مغطي الشوكولاتة بني دائري 1 * 12', 1, 11.00, 11.00, 15.00, 1.65, 12.65),
        (N'10720', N'معمول اصابع شوكولاته والمكسرات مثلثة 1 * 12 سطل', 72, 5.13, 369.36, 15.00, 55.40, 424.76),
        (N'10721', N'معمول اصابع شوكولاته بني مثلثة 1 * 12',       204,  5.13, 1046.52, 15.00, 156.98, 1203.50),
        (N'10722', N'معمول اصابع شوكولاته بيضاء مثلثة 1 * 12',      96,  5.13, 492.48,  15.00, 73.87,  566.35),
        (N'10723', N'معمول اصابع شوكولاته والكابيتشينو مثلثة 1 * 12', 132, 5.13, 677.16, 15.00, 101.57, 778.73),
        (N'1076',  N'معمول اسطمبة التمر 400جم* 12',                 122, 10.05, 1226.10, 15.00, 183.92, 1410.02);

    UPDATE product
    SET product.ErpId = fixture.SupplierSku,
        product.Name = fixture.ProductName,
        product.Description = fixture.ProductName,
        product.UnitPrice = fixture.UnitPrice,
        product.Uom = N'باكت /علبة',
        product.IsDeleted = 0,
        product.UpdatedAt = SYSUTCDATETIME()
    FROM dbo.Products AS product
    INNER JOIN @Items AS fixture
        ON fixture.SupplierSku = product.SkuSupplier
    WHERE product.VendorId = @VendorId;

    INSERT INTO dbo.Products
    (
        ErpId,
        Name,
        SkuSupplier,
        Description,
        UnitPrice,
        Uom,
        VendorId,
        CreatedAt,
        IsDeleted
    )
    SELECT
        fixture.SupplierSku,
        fixture.ProductName,
        fixture.SupplierSku,
        fixture.ProductName,
        fixture.UnitPrice,
        N'باكت /علبة',
        @VendorId,
        SYSUTCDATETIME(),
        0
    FROM @Items AS fixture
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.Products AS product
        WHERE product.VendorId = @VendorId
          AND product.SkuSupplier = fixture.SupplierSku
    );

    DECLARE @OrderNumber nvarchar(256) = N'PO2600514032';
    DECLARE @PurchaseOrderId int;

    SELECT @PurchaseOrderId = Id
    FROM dbo.PurchaseOrders
    WHERE OrderNumber = @OrderNumber
      AND IsDeleted = 0;

    IF @PurchaseOrderId IS NULL
    BEGIN
        INSERT INTO dbo.PurchaseOrders
        (
            OrderNumber,
            VendorId,
            RequestedByUserId,
            Status,
            OrderDate,
            TotalAmount,
            CreatedAt,
            IsDeleted
        )
        VALUES
        (
            @OrderNumber,
            @VendorId,
            @RequestedByUserId,
            0,
            CONVERT(datetime2, '2026-06-25T00:00:00'),
            5294.16,
            SYSUTCDATETIME(),
            0
        );

        SET @PurchaseOrderId = CONVERT(int, SCOPE_IDENTITY());
    END;
    ELSE
    BEGIN
        UPDATE dbo.PurchaseOrders
        SET VendorId = @VendorId,
            TotalAmount = 5294.16,
            OrderDate = CONVERT(datetime2, '2026-06-25T00:00:00'),
            IsDeleted = 0,
            UpdatedAt = SYSUTCDATETIME()
        WHERE Id = @PurchaseOrderId;
    END;

    UPDATE poItem
    SET poItem.Quantity = fixture.Quantity,
        poItem.UnitPrice = fixture.UnitPrice,
        poItem.LineTotal = fixture.LineTotal,
        poItem.VatPercentage = fixture.VatPercentage,
        poItem.VatAmount = fixture.VatAmount,
        poItem.Amount = fixture.Amount,
        poItem.IsDeleted = 0,
        poItem.UpdatedAt = SYSUTCDATETIME()
    FROM dbo.PurchaseOrderItems AS poItem
    INNER JOIN dbo.Products AS product
        ON product.Id = poItem.ProductId
    INNER JOIN @Items AS fixture
        ON fixture.SupplierSku = product.SkuSupplier
    WHERE poItem.PurchaseOrderId = @PurchaseOrderId;

    INSERT INTO dbo.PurchaseOrderItems
    (
        PurchaseOrderId,
        ProductId,
        Quantity,
        UnitPrice,
        LineTotal,
        VatPercentage,
        VatAmount,
        Amount,
        CreatedAt,
        IsDeleted
    )
    SELECT
        @PurchaseOrderId,
        product.Id,
        fixture.Quantity,
        fixture.UnitPrice,
        fixture.LineTotal,
        fixture.VatPercentage,
        fixture.VatAmount,
        fixture.Amount,
        SYSUTCDATETIME(),
        0
    FROM @Items AS fixture
    INNER JOIN dbo.Products AS product
        ON product.VendorId = @VendorId
       AND product.SkuSupplier = fixture.SupplierSku
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.PurchaseOrderItems AS poItem
        WHERE poItem.PurchaseOrderId = @PurchaseOrderId
          AND poItem.ProductId = product.Id
    );

    IF
    (
        SELECT COUNT(*)
        FROM dbo.PurchaseOrderItems
        WHERE PurchaseOrderId = @PurchaseOrderId
          AND IsDeleted = 0
    ) <> 9
        THROW 51002, 'Demo purchase order must contain exactly 9 active items.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM dbo.PurchaseOrderItems
        WHERE PurchaseOrderId = @PurchaseOrderId
          AND
          (
              ABS((LineTotal + VatAmount) - Amount) > 0.01
              OR VatPercentage <> 15.00
          )
    )
        THROW 51003, 'Demo purchase-order item validation failed.', 1;

    IF ABS
    (
        (
            SELECT SUM(Amount)
            FROM dbo.PurchaseOrderItems
            WHERE PurchaseOrderId = @PurchaseOrderId
              AND IsDeleted = 0
        ) - 5294.16
    ) > 0.01
        THROW 51004, 'Demo purchase-order total validation failed.', 1;

    COMMIT TRANSACTION;

    SELECT
        @VendorId AS VendorId,
        @PurchaseOrderId AS PurchaseOrderId,
        9 AS ItemCount,
        CAST(4603.62 AS decimal(18, 2)) AS Subtotal,
        CAST(690.54 AS decimal(18, 2)) AS Vat,
        CAST(5294.16 AS decimal(18, 2)) AS TotalAmount;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;
