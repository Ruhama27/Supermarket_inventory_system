CREATE DATABASE InventoryDB;
USE InventoryDB;
SELECT * FROM Products;
 SELECT 
     ID, 
     Barcode, 
     PName AS [Product Name], 
     Category AS [Product Category], 
     PStatus AS [Product Status], 
     DateAdded AS [Date Added]
 FROM Products;
 CREATE TABLE Stocks (
    stock_id INT IDENTITY(1,1) PRIMARY KEY,
    barcode_id INT,
    stock_code NVARCHAR(100),
    quantity INT,
    minimum_stock INT,
    cp_unit DECIMAL(10,2),    
    sp_unit DECIMAL(10,2),     
    arrival_date DATETIME,
    expiry_date DATETIME NULL,
    date_added DATETIME DEFAULT GETDATE(),

    CONSTRAINT FK_Stock_Product 
        FOREIGN KEY (barcode_id) 
        REFERENCES Products(ID)
);
SELECT * FROM Stocks;
ALTER TABLE Products
ALTER COLUMN Barcode BIGINT;
DROP TABLE Stock;
ALTER TABLE Stocks
ALTER TABLE Stocks
ALTER COLUMN arrival_date DATE;

ALTER TABLE Stocks
ALTER COLUMN expiry_date DATE;
CREATE TABLE Orders (
    OrderID INT IDENTITY(1,1) PRIMARY KEY,
    Date DATE DEFAULT CAST(GETDATE() AS DATE),
    Subtotal DECIMAL(10, 2) NOT NULL,
    DiscountPercent DECIMAL(5, 2) NOT NULL,
    TotalPayable DECIMAL(10, 2) NOT NULL,
    CashReceived DECIMAL(10, 2) NOT NULL,
    ChangeReturned DECIMAL(10, 2) NOT NULL
);
CREATE TABLE OrderDetails (
    OrderDetailID INT IDENTITY(1,1) PRIMARY KEY,
    OrderID INT FOREIGN KEY REFERENCES Orders(OrderID),
    Barcode BIGINT NOT NULL,
    ProductName NVARCHAR(100),
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(10, 2) NOT NULL,
    TotalPrice DECIMAL(10, 2) NOT NULL
);
ALTER TABLE Orders
DROP COLUMN DiscountPercent;
SELECT * FROM OrderDetails;
SELECT * FROM Orders;
DELETE FROM Orders WHERE OrderID = 11

 -------------------------------------------------

 CREATE TABLE AuditLog (
    LogID INT PRIMARY KEY IDENTITY(1,1),
    Message NVARCHAR(500),
    ActionTime DATETIME DEFAULT GETDATE()
);

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
END;

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
END;

--------------------------------------------------------
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

CREATE PROCEDURE sp_GetNeverSoldItems
AS
BEGIN
    SELECT p.PName
    FROM Products p
    WHERE p.Barcode NOT IN (SELECT Barcode FROM OrderDetails)
END

CREATE PROCEDURE sp_GetRecentAuditLogs
AS
BEGIN
    SELECT TOP 10 
        Message,
        ActionTime
    FROM AuditLog
    ORDER BY ActionTime DESC
END
