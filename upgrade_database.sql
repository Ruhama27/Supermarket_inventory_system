-- ============================================================================
-- Stock-Sync Supermarket Management System - Database Migration / Upgrade
--
-- This script safely updates any existing InventoryDB to Schema v2.0
-- without dropping tables or losing existing product, sales, or user data.
-- ============================================================================

USE InventoryDB;
GO

-- 1. Users Table Upgrades
IF COL_LENGTH('Users', 'Password') IS NOT NULL
BEGIN
    ALTER TABLE Users ALTER COLUMN Password NVARCHAR(256) NOT NULL;
END
GO

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
GO

IF COL_LENGTH('Users', 'IsActive') IS NULL
BEGIN
    ALTER TABLE Users ADD IsActive BIT NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT 1;
END
GO

IF COL_LENGTH('Users', 'CreatedAt') IS NULL
BEGIN
    ALTER TABLE Users ADD CreatedAt DATETIME NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT GETDATE();
END
GO

-- 2. Categories Table
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
END
GO

-- 3. Suppliers Table
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
END
GO

-- 4. Products Table Upgrades
IF COL_LENGTH('Products', 'IsActive') IS NULL
BEGIN
    ALTER TABLE Products ADD IsActive BIT NOT NULL CONSTRAINT DF_Products_IsActive DEFAULT 1;
END
GO

IF COL_LENGTH('Products', 'CategoryID') IS NULL
BEGIN
    ALTER TABLE Products ADD CategoryID INT NULL CONSTRAINT FK_Products_Categories FOREIGN KEY REFERENCES Categories(CategoryID);
END
GO

IF COL_LENGTH('Products', 'Brand') IS NULL
BEGIN
    ALTER TABLE Products ADD Brand NVARCHAR(100) NULL;
END
GO

IF COL_LENGTH('Products', 'Unit') IS NULL
BEGIN
    ALTER TABLE Products ADD Unit NVARCHAR(50) NOT NULL CONSTRAINT DF_Products_Unit DEFAULT 'Pcs';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Products_Barcode' AND object_id = OBJECT_ID('Products'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX IX_Products_Barcode ON Products(Barcode);
END
GO

-- 5. Stocks Table Upgrades
IF COL_LENGTH('Stocks', 'IsActive') IS NULL
BEGIN
    ALTER TABLE Stocks ADD IsActive BIT NOT NULL CONSTRAINT DF_Stocks_IsActive DEFAULT 1;
END
GO

IF COL_LENGTH('Stocks', 'supplier_id') IS NULL
BEGIN
    ALTER TABLE Stocks ADD supplier_id INT NULL CONSTRAINT FK_Stocks_Suppliers FOREIGN KEY REFERENCES Suppliers(SupplierID);
END
GO

IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_Stock_Quantity_NonNegative')
BEGIN
    ALTER TABLE Stocks ADD CONSTRAINT CK_Stock_Quantity_NonNegative CHECK (quantity >= 0);
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Stocks_Barcode_Expiry' AND object_id = OBJECT_ID('Stocks'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Stocks_Barcode_Expiry 
    ON Stocks (barcode_id, IsActive, quantity, expiry_date)
    INCLUDE (sp_unit, cp_unit, minimum_stock);
END
GO

-- 6. Stock View Refresh
IF OBJECT_ID('dbo.Stock', 'V') IS NOT NULL
    DROP VIEW dbo.Stock;
GO
CREATE VIEW dbo.Stock AS 
    SELECT * FROM dbo.Stocks WHERE IsActive = 1;
GO

-- 7. StaffShifts Table
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
END
GO

-- 8. Orders Table Upgrades
IF COL_LENGTH('Orders', 'OrderTime') IS NULL
BEGIN
    ALTER TABLE Orders ADD OrderTime DATETIME NOT NULL CONSTRAINT DF_Orders_Time DEFAULT GETDATE();
END
GO

IF COL_LENGTH('Orders', 'DiscountAmount') IS NULL
BEGIN
    ALTER TABLE Orders ADD DiscountAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_Orders_Discount DEFAULT 0.00;
END
GO

IF COL_LENGTH('Orders', 'TaxAmount') IS NULL
BEGIN
    ALTER TABLE Orders ADD TaxAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_Orders_Tax DEFAULT 0.00;
END
GO

IF COL_LENGTH('Orders', 'PaymentMethod') IS NULL
BEGIN
    ALTER TABLE Orders ADD PaymentMethod NVARCHAR(50) NOT NULL CONSTRAINT DF_Orders_Payment DEFAULT 'Cash';
END
GO

IF COL_LENGTH('Orders', 'CashierEmail') IS NULL
BEGIN
    ALTER TABLE Orders ADD CashierEmail NVARCHAR(100) NULL;
END
GO

IF COL_LENGTH('Orders', 'CashierName') IS NULL
BEGIN
    ALTER TABLE Orders ADD CashierName NVARCHAR(100) NULL;
END
GO

IF COL_LENGTH('Orders', 'ShiftID') IS NULL
BEGIN
    ALTER TABLE Orders ADD ShiftID INT NULL CONSTRAINT FK_Orders_StaffShifts FOREIGN KEY REFERENCES StaffShifts(ShiftID);
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Orders_Date' AND object_id = OBJECT_ID('Orders'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Orders_Date ON Orders(Date, OrderID) INCLUDE (TotalPayable, CashierName);
END
GO

-- 9. OrderDetails Table
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
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_OrderDetails_OrderID' AND object_id = OBJECT_ID('OrderDetails'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_OrderDetails_OrderID ON OrderDetails(OrderID, Barcode) INCLUDE (Quantity, UnitPrice, TotalPrice);
END
GO

-- 10. StockMovements Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'StockMovements')
BEGIN
    CREATE TABLE StockMovements (
        MovementID INT IDENTITY(1,1) PRIMARY KEY,
        Barcode BIGINT NOT NULL,
        StockID INT NULL,
        MovementType NVARCHAR(50) NOT NULL,
        QuantityChange INT NOT NULL,
        BalanceAfter INT NULL,
        ReferenceNo NVARCHAR(100) NULL,
        Remarks NVARCHAR(250) NULL,
        PerformedBy NVARCHAR(100) NULL,
        MovementDate DATETIME NOT NULL CONSTRAINT DF_StockMovements_Date DEFAULT GETDATE()
    );
END
GO

-- 11. AuditLog Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AuditLog')
BEGIN
    CREATE TABLE AuditLog (
        LogID INT IDENTITY(1,1) PRIMARY KEY,
        Message NVARCHAR(500) NOT NULL,
        ActionTime DATETIME NOT NULL CONSTRAINT DF_AuditLog_ActionTime DEFAULT GETDATE()
    );
END
GO

PRINT 'Database upgrade completed successfully.';
GO
