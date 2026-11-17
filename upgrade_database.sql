USE InventoryDB;
GO

-- 1. Upgrade Users table
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Users') AND name = 'Password' AND max_length < 256)
BEGIN
    ALTER TABLE Users ALTER COLUMN Password NVARCHAR(256) NOT NULL;
END
GO

-- 2. Add IsActive to Products if not exists
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'IsActive')
BEGIN
    ALTER TABLE Products ADD IsActive BIT NOT NULL DEFAULT 1;
END
GO

-- 3. Add IsActive and non-negative constraint to Stocks if not exists
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Stocks') AND name = 'IsActive')
BEGIN
    ALTER TABLE Stocks ADD IsActive BIT NOT NULL DEFAULT 1;
END
GO

IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_Stock_Quantity_NonNegative')
BEGIN
    ALTER TABLE Stocks ADD CONSTRAINT CK_Stock_Quantity_NonNegative CHECK (quantity >= 0);
END
GO

-- 4. Update Stock view
IF OBJECT_ID('dbo.Stock', 'V') IS NOT NULL
    DROP VIEW dbo.Stock;
GO
CREATE VIEW dbo.Stock AS SELECT * FROM dbo.Stocks WHERE IsActive = 1;
GO

-- 5. Add Retail POS columns to Orders
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Orders') AND name = 'PaymentMethod')
BEGIN
    ALTER TABLE Orders ADD PaymentMethod NVARCHAR(50) NOT NULL DEFAULT 'Cash';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Orders') AND name = 'DiscountAmount')
BEGIN
    ALTER TABLE Orders ADD DiscountAmount DECIMAL(10,2) NOT NULL DEFAULT 0;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Orders') AND name = 'TaxAmount')
BEGIN
    ALTER TABLE Orders ADD TaxAmount DECIMAL(10,2) NOT NULL DEFAULT 0;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Orders') AND name = 'CashierEmail')
BEGIN
    ALTER TABLE Orders ADD CashierEmail NVARCHAR(100) NULL;
END
GO

PRINT 'Database upgrade completed successfully.';
