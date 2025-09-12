-- SQL Script to create OrderItemModifiers table

-- Create OrderItemModifiers Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'OrderItemModifiers')
BEGIN
    CREATE TABLE [dbo].[OrderItemModifiers] (
        [Id] INT IDENTITY(1,1) PRIMARY KEY,
        [OrderItemId] INT NOT NULL,
        [ModifierId] INT NOT NULL,
        [Price] DECIMAL(10, 2) NOT NULL,
        CONSTRAINT [FK_OrderItemModifiers_OrderItems] FOREIGN KEY ([OrderItemId]) REFERENCES [OrderItems]([Id]),
        CONSTRAINT [FK_OrderItemModifiers_Modifiers] FOREIGN KEY ([ModifierId]) REFERENCES [Modifiers]([Id])
    );
    PRINT 'OrderItemModifiers table created successfully';
END
ELSE
BEGIN
    PRINT 'OrderItemModifiers table already exists';
END
GO

-- Alternative table name check
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'OrderItem_Modifiers')
BEGIN
    CREATE TABLE [dbo].[OrderItem_Modifiers] (
        [Id] INT IDENTITY(1,1) PRIMARY KEY,
        [OrderItemId] INT NOT NULL,
        [ModifierId] INT NOT NULL,
        [Price] DECIMAL(10, 2) NOT NULL,
        CONSTRAINT [FK_OrderItem_Modifiers_OrderItems] FOREIGN KEY ([OrderItemId]) REFERENCES [OrderItems]([Id]),
        CONSTRAINT [FK_OrderItem_Modifiers_Modifiers] FOREIGN KEY ([ModifierId]) REFERENCES [Modifiers]([Id])
    );
    PRINT 'OrderItem_Modifiers table created successfully';
END
ELSE
BEGIN
    PRINT 'OrderItem_Modifiers table already exists';
END
GO
