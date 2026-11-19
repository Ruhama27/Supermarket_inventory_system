-- ============================================================================
-- Stock-Sync Supermarket Management System - Unified Database Schema (v2.0)
-- 
-- Description:
--   Comprehensive, idempotent SQL Server initialization & migration script.
--   Combines base schema, retail POS upgrades, staff shift management,
--   audit logging, performance indexes, and PBKDF2 pre-hashed credentials.
--
-- Safe to run on:
--   1. A brand new, clean SQL Server instance (fresh installation)
--   2. An existing database with prior data (seamless in-place upgrade)
-- ============================================================================

-- ----------------------------------------------------------------------------
-- 1. Database Creation
-- ----------------------------------------------------------------------------
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'InventoryDB')
BEGIN
    CREATE DATABASE InventoryDB;
    PRINT 'Database InventoryDB created.';
END
GO

USE InventoryDB;
GO

-- ----------------------------------------------------------------------------
-- 2. Users Table (Role-Based Authentication with PBKDF2 Hashing)
-- ----------------------------------------------------------------------------
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
BEGIN
    CREATE TABLE Users (
        UserID INT IDENTITY(1,1) PRIMARY KEY,
        Username NVARCHAR(100) NOT NULL UNIQUE,
        Email NVARCHAR(100) NOT NULL UNIQUE,
        Password NVARCHAR(256) NOT NULL,
        Role NVARCHAR(50) NOT NULL CONSTRAINT DF_Users_Role DEFAULT 'Staff',
        IsActive BIT NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT 1,
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT GETDATE()
    );

    -- Seed initial admin and staff accounts with PBKDF2 pre-hashed passwords
    -- Admin credentials: username = 'admin', password = 'admin123'
    -- Staff credentials: username = 'staff', password = 'staff123'
    INSERT INTO Users (Username, Email, Password, Role, IsActive) VALUES
    ('admin', 'admin@gmail.com', 'PBKDF2$50000$s8xwtTNw3Jdgyoorepb6Hw==$WVnaFfRbnuLSc5HPgIHMCQcCN1fleuxlVRAUUd71ryw=', 'Admin', 1),
    ('staff', 'staff@gmail.com', 'PBKDF2$50000$LTcYXqQMQipuDJ7Bn9Vv2A==$hE9e1tN+05HINwO018DnPapyrnMGjywNUMX9pDDpqSg=', 'Staff', 1);

    PRINT 'Users table created with pre-hashed PBKDF2 initial credentials.';
END
ELSE
BEGIN
    -- Upgrade existing Users table in-place
    IF COL_LENGTH('Users', 'Password') IS NOT NULL
    BEGIN
        ALTER TABLE Users ALTER COLUMN Password NVARCHAR(256) NOT NULL;
    END

    IF COL_LENGTH('Users', 'Username') IS NULL
    BEGIN
        ALTER TABLE Users ADD Username NVARCHAR(100) NULL;
        EXEC('UPDATE Users SET Username = CASE 
                WHEN Email = ''admin@gmail.com'' THEN ''admin''
                WHEN Email = ''staff@gmail.com'' THEN ''staff''
                ELSE SUBSTRING(Email, 1, CHARINDEX(''@'', Email + ''@'') - 1)
              END WHERE Username IS NULL OR Username = ''''');
        ALTER TABLE Users ALTER COLUMN Username NVARCHAR(100) NOT NULL;
    END

    IF COL_LENGTH('Users', 'IsActive') IS NULL
    BEGIN
        ALTER TABLE Users ADD IsActive BIT NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT 1;
    END

    IF COL_LENGTH('Users', 'CreatedAt') IS NULL
    BEGIN
        ALTER TABLE Users ADD CreatedAt DATETIME NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT GETDATE();
    END

    PRINT 'Users table verified/upgraded.';
END
GO

-- ----------------------------------------------------------------------------
-- 3. Categories Table (Product Taxonomy & Organization)
-- ----------------------------------------------------------------------------
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Categories')
BEGIN
    CREATE TABLE Categories (
        CategoryID INT IDENTITY(1,1) PRIMARY KEY,
        CategoryName NVARCHAR(100) NOT NULL UNIQUE,
        Description NVARCHAR(250) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_Categories_IsActive DEFAULT 1,
        DateAdded DATETIME NOT NULL CONSTRAINT DF_Categories_DateAdded DEFAULT GETDATE()
    );

    INSERT INTO Categories (CategoryName, Description) VALUES
    ('Dairy', 'Milk, yogurt, cheeses, eggs, and butter'),
    ('Bakery', 'Fresh breads, pastries, buns, and cakes'),
    ('Beverages', 'Juices, water, sodas, and soft drinks'),
    ('Snacks', 'Chips, biscuits, candy, and nuts'),
    ('Produce', 'Fresh fruits and vegetables'),
    ('Canned Goods', 'Canned vegetables, soups, and beans'),
    ('Household', 'Cleaning supplies and paper products'),
    ('Personal Care', 'Soap, shampoo, toothpaste, and toiletries');

    PRINT 'Categories table created and seeded.';
END
GO

-- ----------------------------------------------------------------------------
-- 4. Suppliers Table (Vendor & Purchasing Management)
-- ----------------------------------------------------------------------------
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Suppliers')
BEGIN
    CREATE TABLE Suppliers (
        SupplierID INT IDENTITY(1,1) PRIMARY KEY,
        SupplierName NVARCHAR(150) NOT NULL UNIQUE,
        ContactPerson NVARCHAR(100) NULL,
        Phone NVARCHAR(50) NULL,
        Email NVARCHAR(100) NULL,
        Address NVARCHAR(250) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_Suppliers_IsActive DEFAULT 1,
        DateAdded DATETIME NOT NULL CONSTRAINT DF_Suppliers_DateAdded DEFAULT GETDATE()
    );

    INSERT INTO Suppliers (SupplierName, ContactPerson, Phone, Email, Address) VALUES
    ('National Dairy & Foods Ltd.', 'Abebe Tadesse', '+251 911 223344', 'sales@nationaldairy.et', 'Addis Ababa, Bole'),
    ('Addis Central Bakery Supplies', 'Sara Mengistu', '+251 912 334455', 'orders@addisbakery.et', 'Addis Ababa, Piazza'),
    ('Highland Beverages Corp.', 'Dawit Yohannes', '+251 913 445566', 'info@highlandbev.et', 'Addis Ababa, Kaliti');

    PRINT 'Suppliers table created and seeded.';
END
GO

-- ----------------------------------------------------------------------------
-- 5. Products Table (Master Product Catalog)
-- ----------------------------------------------------------------------------
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Products')
BEGIN
    CREATE TABLE Products (
        ID INT IDENTITY(1,1) PRIMARY KEY,
        Barcode BIGINT NOT NULL UNIQUE,
        PName NVARCHAR(100) NOT NULL,
        Category NVARCHAR(100) NULL,
        CategoryID INT NULL CONSTRAINT FK_Products_Categories FOREIGN KEY REFERENCES Categories(CategoryID),
        Brand NVARCHAR(100) NULL,
        Unit NVARCHAR(50) NOT NULL CONSTRAINT DF_Products_Unit DEFAULT 'Pcs',
        PStatus NVARCHAR(50) NOT NULL CONSTRAINT DF_Products_PStatus DEFAULT 'Available',
        IsActive BIT NOT NULL CONSTRAINT DF_Products_IsActive DEFAULT 1,
        DateAdded DATETIME NOT NULL CONSTRAINT DF_Products_DateAdded DEFAULT GETDATE()
    );

    PRINT 'Products table created.';
END
ELSE
BEGIN
    IF COL_LENGTH('Products', 'IsActive') IS NULL
    BEGIN
        ALTER TABLE Products ADD IsActive BIT NOT NULL CONSTRAINT DF_Products_IsActive DEFAULT 1;
    END
    IF COL_LENGTH('Products', 'CategoryID') IS NULL
    BEGIN
        ALTER TABLE Products ADD CategoryID INT NULL CONSTRAINT FK_Products_Categories FOREIGN KEY REFERENCES Categories(CategoryID);
    END
    IF COL_LENGTH('Products', 'Brand') IS NULL
    BEGIN
        ALTER TABLE Products ADD Brand NVARCHAR(100) NULL;
    END
    IF COL_LENGTH('Products', 'Unit') IS NULL
    BEGIN
        ALTER TABLE Products ADD Unit NVARCHAR(50) NOT NULL CONSTRAINT DF_Products_Unit DEFAULT 'Pcs';
    END
    PRINT 'Products table verified/upgraded.';
END
GO

-- Create index on Barcode if not exists
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Products_Barcode' AND object_id = OBJECT_ID('Products'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX IX_Products_Barcode ON Products(Barcode);
    PRINT 'Index IX_Products_Barcode created.';
END
GO

-- ----------------------------------------------------------------------------
-- 6. Stocks Table (Batch-Specific Inventory with Cost & Expiry)
-- ----------------------------------------------------------------------------
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Stocks')
BEGIN
    CREATE TABLE Stocks (
        stock_id INT IDENTITY(1,1) PRIMARY KEY,
        barcode_id INT NOT NULL,
        stock_code NVARCHAR(100) NULL,
        supplier_id INT NULL CONSTRAINT FK_Stocks_Suppliers FOREIGN KEY REFERENCES Suppliers(SupplierID),
        quantity INT NOT NULL CONSTRAINT CK_Stock_Quantity_NonNegative CHECK (quantity >= 0),
        minimum_stock INT NOT NULL CONSTRAINT DF_Stocks_MinStock DEFAULT 5,
        cp_unit DECIMAL(18,2) NOT NULL CONSTRAINT DF_Stocks_CP DEFAULT 0.00,
        sp_unit DECIMAL(18,2) NOT NULL CONSTRAINT DF_Stocks_SP DEFAULT 0.00,
        arrival_date DATE NOT NULL CONSTRAINT DF_Stocks_ArrivalDate DEFAULT CAST(GETDATE() AS DATE),
        expiry_date DATE NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_Stocks_IsActive DEFAULT 1,
        date_added DATETIME NOT NULL CONSTRAINT DF_Stocks_DateAdded DEFAULT GETDATE(),
        CONSTRAINT FK_Stock_Product FOREIGN KEY (barcode_id) REFERENCES Products(ID)
    );

    PRINT 'Stocks table created.';
END
ELSE
BEGIN
    IF COL_LENGTH('Stocks', 'IsActive') IS NULL
    BEGIN
        ALTER TABLE Stocks ADD IsActive BIT NOT NULL CONSTRAINT DF_Stocks_IsActive DEFAULT 1;
    END

    IF COL_LENGTH('Stocks', 'supplier_id') IS NULL
    BEGIN
        ALTER TABLE Stocks ADD supplier_id INT NULL CONSTRAINT FK_Stocks_Suppliers FOREIGN KEY REFERENCES Suppliers(SupplierID);
    END

    IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_Stock_Quantity_NonNegative')
    BEGIN
        ALTER TABLE Stocks ADD CONSTRAINT CK_Stock_Quantity_NonNegative CHECK (quantity >= 0);
    END

    PRINT 'Stocks table verified/upgraded.';
END
GO

-- Create performance index on Stocks for FIFO/FEFO query speed
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Stocks_Barcode_Expiry' AND object_id = OBJECT_ID('Stocks'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Stocks_Barcode_Expiry 
    ON Stocks (barcode_id, IsActive, quantity, expiry_date)
    INCLUDE (sp_unit, cp_unit, minimum_stock);
    PRINT 'Index IX_Stocks_Barcode_Expiry created.';
END
GO

-- ----------------------------------------------------------------------------
-- 7. Stock View (Backwards Compatibility for Older Modules)
-- ----------------------------------------------------------------------------
IF OBJECT_ID('dbo.Stock', 'V') IS NOT NULL
    DROP VIEW dbo.Stock;
GO
CREATE VIEW dbo.Stock AS 
    SELECT * FROM dbo.Stocks WHERE IsActive = 1;
GO
PRINT 'Stock view refreshed.';
GO

-- ----------------------------------------------------------------------------
-- 8. StaffShifts Table (Shift & Cashier Accountability Tracking)
-- ----------------------------------------------------------------------------
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'StaffShifts')
BEGIN
    CREATE TABLE StaffShifts (
        ShiftID INT IDENTITY(1,1) PRIMARY KEY,
        UserID INT NOT NULL,
        Username NVARCHAR(100) NOT NULL,
        Role NVARCHAR(50) NOT NULL,
        ShiftStart DATETIME NOT NULL CONSTRAINT DF_StaffShifts_Start DEFAULT GETDATE(),
        ShiftEnd DATETIME NULL,
        TotalSalesCount INT NOT NULL CONSTRAINT DF_StaffShifts_Count DEFAULT 0,
        TotalSalesAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_StaffShifts_Amount DEFAULT 0.00,
        Status NVARCHAR(50) NOT NULL CONSTRAINT DF_StaffShifts_Status DEFAULT 'Active'
    );
    PRINT 'StaffShifts table created.';
END
GO

-- ----------------------------------------------------------------------------
-- 9. Orders Table (Retail Sales Transactions)
-- ----------------------------------------------------------------------------
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Orders')
BEGIN
    CREATE TABLE Orders (
        OrderID INT IDENTITY(1,1) PRIMARY KEY,
        Date DATE NOT NULL CONSTRAINT DF_Orders_Date DEFAULT CAST(GETDATE() AS DATE),
        OrderTime DATETIME NOT NULL CONSTRAINT DF_Orders_Time DEFAULT GETDATE(),
        Subtotal DECIMAL(18,2) NOT NULL,
        DiscountAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_Orders_Discount DEFAULT 0.00,
        TaxAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_Orders_Tax DEFAULT 0.00,
        TotalPayable DECIMAL(18,2) NOT NULL,
        CashReceived DECIMAL(18,2) NOT NULL,
        ChangeReturned DECIMAL(18,2) NOT NULL,
        PaymentMethod NVARCHAR(50) NOT NULL CONSTRAINT DF_Orders_Payment DEFAULT 'Cash',
        CashierEmail NVARCHAR(100) NULL,
        CashierName NVARCHAR(100) NULL,
        ShiftID INT NULL CONSTRAINT FK_Orders_StaffShifts FOREIGN KEY REFERENCES StaffShifts(ShiftID)
    );
    PRINT 'Orders table created.';
END
ELSE
BEGIN
    IF COL_LENGTH('Orders', 'OrderTime') IS NULL
    BEGIN
        ALTER TABLE Orders ADD OrderTime DATETIME NOT NULL CONSTRAINT DF_Orders_Time DEFAULT GETDATE();
    END
    IF COL_LENGTH('Orders', 'DiscountAmount') IS NULL
    BEGIN
        ALTER TABLE Orders ADD DiscountAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_Orders_Discount DEFAULT 0.00;
    END
    IF COL_LENGTH('Orders', 'TaxAmount') IS NULL
    BEGIN
        ALTER TABLE Orders ADD TaxAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_Orders_Tax DEFAULT 0.00;
    END
    IF COL_LENGTH('Orders', 'PaymentMethod') IS NULL
    BEGIN
        ALTER TABLE Orders ADD PaymentMethod NVARCHAR(50) NOT NULL CONSTRAINT DF_Orders_Payment DEFAULT 'Cash';
    END
    IF COL_LENGTH('Orders', 'CashierEmail') IS NULL
    BEGIN
        ALTER TABLE Orders ADD CashierEmail NVARCHAR(100) NULL;
    END
    IF COL_LENGTH('Orders', 'CashierName') IS NULL
    BEGIN
        ALTER TABLE Orders ADD CashierName NVARCHAR(100) NULL;
    END
    IF COL_LENGTH('Orders', 'ShiftID') IS NULL
    BEGIN
        ALTER TABLE Orders ADD ShiftID INT NULL CONSTRAINT FK_Orders_StaffShifts FOREIGN KEY REFERENCES StaffShifts(ShiftID);
    END
    PRINT 'Orders table verified/upgraded.';
END
GO

-- Index on Orders Date for rapid financial & dashboard reports
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Orders_Date' AND object_id = OBJECT_ID('Orders'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Orders_Date ON Orders(Date, OrderID) INCLUDE (TotalPayable, CashierName);
    PRINT 'Index IX_Orders_Date created.';
END
GO

-- ----------------------------------------------------------------------------
-- 10. OrderDetails Table (Itemized Line Items)
-- ----------------------------------------------------------------------------
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'OrderDetails')
BEGIN
    CREATE TABLE OrderDetails (
        OrderDetailID INT IDENTITY(1,1) PRIMARY KEY,
        OrderID INT NOT NULL CONSTRAINT FK_OrderDetails_Orders FOREIGN KEY REFERENCES Orders(OrderID),
        Barcode BIGINT NOT NULL,
        ProductName NVARCHAR(100) NOT NULL,
        Quantity INT NOT NULL,
        UnitPrice DECIMAL(18,2) NOT NULL,
        TotalPrice DECIMAL(18,2) NOT NULL
    );
    PRINT 'OrderDetails table created.';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_OrderDetails_OrderID' AND object_id = OBJECT_ID('OrderDetails'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_OrderDetails_OrderID ON OrderDetails(OrderID, Barcode) INCLUDE (Quantity, UnitPrice, TotalPrice);
    PRINT 'Index IX_OrderDetails_OrderID created.';
END
GO

-- ----------------------------------------------------------------------------
-- 11. StockMovements Table (Inventory Audit Log for In/Out/Damages/Returns)
-- ----------------------------------------------------------------------------
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'StockMovements')
BEGIN
    CREATE TABLE StockMovements (
        MovementID INT IDENTITY(1,1) PRIMARY KEY,
        Barcode BIGINT NOT NULL,
        StockID INT NULL,
        MovementType NVARCHAR(50) NOT NULL, -- 'Sale', 'Restock', 'Damage', 'Return', 'Adjustment'
        QuantityChange INT NOT NULL,        -- Positive for restock/return, negative for sale/damage
        BalanceAfter INT NULL,
        ReferenceNo NVARCHAR(100) NULL,     -- Order #, Invoice #, or Adjustment Ref
        Remarks NVARCHAR(250) NULL,
        PerformedBy NVARCHAR(100) NULL,
        MovementDate DATETIME NOT NULL CONSTRAINT DF_StockMovements_Date DEFAULT GETDATE()
    );
    PRINT 'StockMovements table created.';
END
GO

-- ----------------------------------------------------------------------------
-- 12. AuditLog Table (General System Security & Activity Trail)
-- ----------------------------------------------------------------------------
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AuditLog')
BEGIN
    CREATE TABLE AuditLog (
        LogID INT IDENTITY(1,1) PRIMARY KEY,
        Message NVARCHAR(500) NOT NULL,
        ActionTime DATETIME NOT NULL CONSTRAINT DF_AuditLog_ActionTime DEFAULT GETDATE()
    );
    PRINT 'AuditLog table created.';
END
GO

-- ----------------------------------------------------------------------------
-- 13. Audit Triggers
-- ----------------------------------------------------------------------------
IF OBJECT_ID('dbo.trg_Products_Audit', 'TR') IS NOT NULL
    DROP TRIGGER dbo.trg_Products_Audit;
GO
CREATE TRIGGER trg_Products_Audit
ON Products
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT * FROM inserted) AND NOT EXISTS (SELECT * FROM deleted)
    BEGIN
        INSERT INTO AuditLog (Message)
        SELECT 'Product Added: ' + PName + ' | Barcode: ' + CAST(Barcode AS NVARCHAR)
        FROM inserted;
    END

    IF EXISTS (SELECT * FROM deleted) AND NOT EXISTS (SELECT * FROM inserted)
    BEGIN
        INSERT INTO AuditLog (Message)
        SELECT 'Product Deleted: ' + PName + ' | Barcode: ' + CAST(Barcode AS NVARCHAR)
        FROM deleted;
    END

    IF EXISTS (SELECT * FROM inserted) AND EXISTS (SELECT * FROM deleted)
    BEGIN
        INSERT INTO AuditLog (Message)
        SELECT 'Product Updated: ' + i.PName + ' | Barcode: ' + CAST(i.Barcode AS NVARCHAR)
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
        SELECT 'Stock Batch Added: ' + p.PName + ' | Barcode: ' + CAST(p.Barcode AS NVARCHAR) + ' | Qty: ' + CAST(i.quantity AS NVARCHAR)
        FROM inserted i
        INNER JOIN Products p ON i.barcode_id = p.ID;
    END

    IF EXISTS (SELECT * FROM deleted) AND NOT EXISTS (SELECT * FROM inserted)
    BEGIN
        INSERT INTO AuditLog (Message)
        SELECT 'Stock Batch Deleted: ' + p.PName + ' | Barcode: ' + CAST(p.Barcode AS NVARCHAR)
        FROM deleted d
        INNER JOIN Products p ON d.barcode_id = p.ID;
    END

    IF EXISTS (SELECT * FROM inserted) AND EXISTS (SELECT * FROM deleted)
    BEGIN
        INSERT INTO AuditLog (Message)
        SELECT 'Stock Batch Updated: ' + p.PName + ' | Barcode: ' + CAST(p.Barcode AS NVARCHAR) + ' | New Qty: ' + CAST(i.quantity AS NVARCHAR)
        FROM inserted i
        INNER JOIN Products p ON i.barcode_id = p.ID;
    END
END
GO

-- ----------------------------------------------------------------------------
-- 14. Core Stored Procedures
-- ----------------------------------------------------------------------------
IF OBJECT_ID('dbo.sp_GetLowStockProducts', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetLowStockProducts;
GO
CREATE PROCEDURE sp_GetLowStockProducts
AS
BEGIN
    SET NOCOUNT ON;
    SELECT 
        p.Barcode,
        p.PName,
        SUM(s.quantity) AS TotalQty,
        MAX(s.minimum_stock) AS MinStock
    FROM Products p
    INNER JOIN Stocks s ON p.ID = s.barcode_id
    WHERE s.IsActive = 1 AND p.IsActive = 1
    GROUP BY p.Barcode, p.PName
    HAVING SUM(s.quantity) <= MAX(s.minimum_stock);
END
GO

IF OBJECT_ID('dbo.sp_GetExpiredProducts', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetExpiredProducts;
GO
CREATE PROCEDURE sp_GetExpiredProducts
AS
BEGIN
    SET NOCOUNT ON;
    SELECT 
        p.Barcode,
        p.PName,
        s.stock_code,
        s.quantity,
        s.expiry_date
    FROM Products p
    INNER JOIN Stocks s ON p.ID = s.barcode_id
    WHERE s.IsActive = 1 AND s.expiry_date <= CAST(GETDATE() AS DATE);
END
GO

IF OBJECT_ID('dbo.sp_GetRecentAuditLogs', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetRecentAuditLogs;
GO
CREATE PROCEDURE sp_GetRecentAuditLogs
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP 20 
        LogID,
        Message,
        ActionTime
    FROM AuditLog
    ORDER BY ActionTime DESC;
END
GO

-- ----------------------------------------------------------------------------
-- 15. Initial Sample Products & Stock (Only if Products table is empty)
-- ----------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM Products)
BEGIN
    INSERT INTO Products (Barcode, PName, Category, Brand, Unit, PStatus) VALUES
    (1001, 'Whole Milk 1L', 'Dairy', 'Family Fresh', 'Bottle', 'Available'),
    (1002, 'White Sliced Bread', 'Bakery', 'Golden Bakery', 'Loaf', 'Available'),
    (1003, 'Organic Fresh Eggs (12pk)', 'Dairy', 'Highland Farms', 'Pack', 'Available'),
    (1004, 'Cheddar Cheese 200g', 'Dairy', 'Anchor', 'Block', 'Available'),
    (1005, 'Orange Juice 1L', 'Beverages', 'SunRype', 'Bottle', 'Available'),
    (1006, 'Mineral Water 500ml', 'Beverages', 'Highland', 'Bottle', 'Available'),
    (1007, 'Potato Crisps Salted 150g', 'Snacks', 'Crunchies', 'Bag', 'Available');

    -- Insert batches with prices in Ethiopian Birr (ETB)
    INSERT INTO Stocks (barcode_id, stock_code, supplier_id, quantity, minimum_stock, cp_unit, sp_unit, arrival_date, expiry_date) VALUES
    (1, 'BAT-2026-001', 1, 60, 15, 65.00, 85.00, CAST(GETDATE() AS DATE), DATEADD(day, 14, CAST(GETDATE() AS DATE))),
    (2, 'BAT-2026-002', 2, 40, 10, 35.00, 50.00, CAST(GETDATE() AS DATE), DATEADD(day, 5, CAST(GETDATE() AS DATE))),
    (3, 'BAT-2026-003', 1, 8, 10, 180.00, 230.00, CAST(GETDATE() AS DATE), DATEADD(day, 21, CAST(GETDATE() AS DATE))),
    (4, 'BAT-2026-004', 1, 25, 8, 120.00, 160.00, CAST(GETDATE() AS DATE), DATEADD(day, 45, CAST(GETDATE() AS DATE))),
    (5, 'BAT-2026-005', 3, 35, 10, 80.00, 110.00, CAST(GETDATE() AS DATE), DATEADD(day, 60, CAST(GETDATE() AS DATE))),
    (6, 'BAT-2026-006', 3, 100, 20, 15.00, 25.00, CAST(GETDATE() AS DATE), DATEADD(day, 180, CAST(GETDATE() AS DATE))),
    (7, 'BAT-2026-007', 1, 50, 15, 45.00, 65.00, CAST(GETDATE() AS DATE), DATEADD(day, 90, CAST(GETDATE() AS DATE)));

    PRINT 'Initial sample supermarket products and stock batches created.';
END
GO

PRINT '============================================================================';
PRINT 'Stock-Sync Database Unified Schema (v2.0) applied successfully!';
PRINT 'Ready for POS terminals, Desktop admin, and Mobile Dashboard.';
PRINT '============================================================================';
GO
