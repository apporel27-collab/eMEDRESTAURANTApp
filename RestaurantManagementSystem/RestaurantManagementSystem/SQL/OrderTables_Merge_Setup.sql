-- Supports merging multiple tables into a single order
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'OrderTables')
BEGIN
    CREATE TABLE OrderTables (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        OrderId INT NOT NULL,
        TableId INT NOT NULL,
        CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_OrderTables_Orders FOREIGN KEY (OrderId) REFERENCES Orders(Id),
        CONSTRAINT FK_OrderTables_Tables FOREIGN KEY (TableId) REFERENCES Tables(Id),
        CONSTRAINT UQ_OrderTables_Order_Table UNIQUE (OrderId, TableId)
    );
END
GO

-- View to quickly show aggregated table names for an order
IF OBJECT_ID('vw_OrderMergedTables') IS NOT NULL
    DROP VIEW vw_OrderMergedTables;
GO
CREATE VIEW vw_OrderMergedTables AS
SELECT o.Id AS OrderId,
       STRING_AGG(t.TableName, ' + ') WITHIN GROUP (ORDER BY t.TableName) AS MergedTableNames
FROM Orders o
LEFT JOIN OrderTables ot ON o.Id = ot.OrderId
LEFT JOIN Tables t ON ot.TableId = t.Id
GROUP BY o.Id;
GO
