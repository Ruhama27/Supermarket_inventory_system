IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'InventoryDB')
BEGIN
    CREATE DATABASE InventoryDB;
END
GO

USE InventoryDB;
GO

-- 1. Users table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
BEGIN
    CREATE TABLE Users (
        UserID INT IDENTITY(1,1) PRIMARY KEY,
        Email NVARCHAR(100) NOT NULL UNIQUE,
        Password NVARCHAR(100) NOT NULL,
        Role NVARCHAR(50) NOT NULL
    );

    INSERT INTO Users (Email, Password, Role) VALUES
    ('admin@gmail.com', 'admin123', 'Admin'),
    ('staff@gmail.com', 'staff123', 'Staff');
END
GO

-- 2. Products table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Products')
BEGIN
    CREATE TABLE Products (
        ID INT IDENTITY(1,1) PRIMARY KEY,
        Barcode BIGINT NOT NULL UNIQUE,
        PName NVARCHAR(100) NOT NULL,
        Category NVARCHAR(100),
        PStatus NVARCHAR(50),
        DateAdded DATETIME DEFAULT GETDATE()
    );
END
GO

-- 3. Stocks table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Stocks')
BEGIN
    CREATE TABLE Stocks (
        stock_id INT IDENTITY(1,1) PRIMARY KEY,
        barcode_id INT NOT NULL,
        stock_code NVARCHAR(100),
        quantity INT NOT NULL,
        minimum_stock INT NOT NULL,
        cp_unit DECIMAL(10,2),    
        sp_unit DECIMAL(10,2),     
        arrival_date DATE,
        expiry_date DATE NULL,
        date_added DATETIME DEFAULT GETDATE(),
        CONSTRAINT FK_Stock_Product FOREIGN KEY (barcode_id) REFERENCES Products(ID)
    );
END
GO

-- Stock view alias for backwards compatibility
IF OBJECT_ID('dbo.Stock', 'V') IS NOT NULL
    DROP VIEW dbo.Stock;
GO
CREATE VIEW dbo.Stock AS SELECT * FROM dbo.Stocks;
GO

-- 4. Orders table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Orders')
BEGIN
    CREATE TABLE Orders (
        OrderID INT IDENTITY(1,1) PRIMARY KEY,
        Date DATE DEFAULT CAST(GETDATE() AS DATE),
        Subtotal DECIMAL(10, 2) NOT NULL,
        TotalPayable DECIMAL(10, 2) NOT NULL,
        CashReceived DECIMAL(10, 2) NOT NULL,
        ChangeReturned DECIMAL(10, 2) NOT NULL
    );
END
GO

-- 5. OrderDetails table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'OrderDetails')
BEGIN
    CREATE TABLE OrderDetails (
        OrderDetailID INT IDENTITY(1,1) PRIMARY KEY,
        OrderID INT FOREIGN KEY REFERENCES Orders(OrderID),
        Barcode BIGINT NOT NULL,
        ProductName NVARCHAR(100),
        Quantity INT NOT NULL,
        UnitPrice DECIMAL(10, 2) NOT NULL,
        TotalPrice DECIMAL(10, 2) NOT NULL
    );
END
GO

-- 6. AuditLog table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AuditLog')
BEGIN
    CREATE TABLE AuditLog (
        LogID INT PRIMARY KEY IDENTITY(1,1),
        Message NVARCHAR(500),
        ActionTime DATETIME DEFAULT GETDATE()
    );
END
GO

-- 7. Triggers
IF OBJECT_ID('dbo.trg_Products_Audit', 'TR') IS NOT NULL
    DROP TRIGGER dbo.trg_Products_Audit;
GO
CREATE TRIGGER trg_Products_Audit
ON Products
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    -- INSERT
    IF EXISTS (SELECT * FROM inserted) AND NOT EXISTS (SELECT * FROM deleted)
    BEGIN
        INSERT INTO AuditLog (Message)
        SELECT 
            'Product Added: ' + PName + 
            ' | Barcode: ' + CAST(Barcode AS NVARCHAR)
        FROM inserted;
    END

    -- DELETE
    IF EXISTS (SELECT * FROM deleted) AND NOT EXISTS (SELECT * FROM inserted)
    BEGIN
        INSERT INTO AuditLog (Message)
        SELECT 
            'Product Deleted: ' + PName + 
            ' | Barcode: ' + CAST(Barcode AS NVARCHAR)
        FROM deleted;
    END

    -- UPDATE
    IF EXISTS (SELECT * FROM inserted) AND EXISTS (SELECT * FROM deleted)
    BEGIN
        INSERT INTO AuditLog (Message)
        SELECT 
            'Product Updated: ' + i.PName + 
            ' | Barcode: ' + CAST(i.Barcode AS NVARCHAR)
        FROM inserted i;
    END
END
GO

IF OBJECT_ID('dbo.trg_Stocks_Audit', 'TR') IS NOT NULL
    DROP TRIGGER dbo.trg_Stocks_Audit;
GO
CREATE TRIGGER trg_Stocks_Audit
ON Stocks
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT * FROM inserted) AND NOT EXISTS (SELECT * FROM deleted)
    BEGIN
        INSERT INTO AuditLog (Message)
        SELECT 
            'Stock Added: ' + p.PName +
            ' | Barcode: ' + CAST(p.Barcode AS NVARCHAR) +
            ' | Quantity: ' + CAST(i.quantity AS NVARCHAR)
        FROM inserted i
        INNER JOIN Products p ON i.barcode_id = p.ID;
    END

    IF EXISTS (SELECT * FROM deleted) AND NOT EXISTS (SELECT * FROM inserted)
    BEGIN
        INSERT INTO AuditLog (Message)
        SELECT 
            'Stock Deleted: ' + p.PName +
            ' | Barcode: ' + CAST(p.Barcode AS NVARCHAR)
        FROM deleted d
        INNER JOIN Products p ON d.barcode_id = p.ID;
    END

    IF EXISTS (SELECT * FROM inserted) AND EXISTS (SELECT * FROM deleted)
    BEGIN
        INSERT INTO AuditLog (Message)
        SELECT 
            'Stock Updated: ' + p.PName +
            ' | Barcode: ' + CAST(p.Barcode AS NVARCHAR) +
            ' | New Qty: ' + CAST(i.quantity AS NVARCHAR)
        FROM inserted i
        INNER JOIN Products p ON i.barcode_id = p.ID;
    END
END
GO

-- 8. Stored Procedures
IF OBJECT_ID('dbo.sp_GetLowStockProducts', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetLowStockProducts;
GO
CREATE PROCEDURE sp_GetLowStockProducts
AS
BEGIN
    SELECT 
        p.PName,
        s.quantity,
        s.minimum_stock
    FROM Products p
    INNER JOIN Stocks s ON p.ID = s.barcode_id
    WHERE s.quantity < s.minimum_stock
END
GO

IF OBJECT_ID('dbo.sp_GetExpiredProducts', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetExpiredProducts;
GO
CREATE PROCEDURE sp_GetExpiredProducts
AS
BEGIN
    SELECT 
        p.PName AS ProductName,
        s.expiry_date
    FROM Products p
    INNER JOIN Stocks s ON p.ID = s.barcode_id
    WHERE s.expiry_date <= GETDATE()
END
GO

IF OBJECT_ID('dbo.sp_GetNeverSoldItems', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetNeverSoldItems;
GO
CREATE PROCEDURE sp_GetNeverSoldItems
AS
BEGIN
    SELECT p.PName
    FROM Products p
    WHERE p.Barcode NOT IN (SELECT Barcode FROM OrderDetails)
END
GO

IF OBJECT_ID('dbo.sp_GetRecentAuditLogs', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetRecentAuditLogs;
GO
CREATE PROCEDURE sp_GetRecentAuditLogs
AS
BEGIN
    SELECT TOP 10 
        Message,
        ActionTime
    FROM AuditLog
    ORDER BY ActionTime DESC
END
GO

-- 9. Sample initial products and stock if empty
IF NOT EXISTS (SELECT 1 FROM Products)
BEGIN
    INSERT INTO Products (Barcode, PName, Category, PStatus) VALUES
    (1001, 'Whole Milk 1L', 'Dairy', 'Available'),
    (1002, 'White Bread', 'Bakery', 'Available'),
    (1003, 'Organic Eggs (12pk)', 'Dairy', 'Available'),
    (1004, 'Cheddar Cheese 200g', 'Dairy', 'Available'),
    (1005, 'Orange Juice 1L', 'Beverages', 'Available');

    INSERT INTO Stocks (barcode_id, stock_code, quantity, minimum_stock, cp_unit, sp_unit, arrival_date, expiry_date) VALUES
    (1, 'STK-001', 50, 10, 1.20, 1.80, CAST(GETDATE() AS DATE), DATEADD(day, 14, CAST(GETDATE() AS DATE))),
    (2, 'STK-002', 30, 5, 0.80, 1.50, CAST(GETDATE() AS DATE), DATEADD(day, 7, CAST(GETDATE() AS DATE))),
    (3, 'STK-003', 4, 10, 2.00, 3.20, CAST(GETDATE() AS DATE), DATEADD(day, 20, CAST(GETDATE() AS DATE))),
    (4, 'STK-004', 15, 5, 2.50, 4.00, CAST(GETDATE() AS DATE), DATEADD(day, -2, CAST(GETDATE() AS DATE))),
    (5, 'STK-005', 25, 5, 1.50, 2.50, CAST(GETDATE() AS DATE), DATEADD(day, 60, CAST(GETDATE() AS DATE)));
END
GO
