using Microsoft.EntityFrameworkCore.Migrations;

namespace RestaurantManagementSystem.Migrations
{
    public partial class AddMenuItemModifiers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Check if MenuItem_Modifiers table exists
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'MenuItem_Modifiers')
                BEGIN
                    CREATE TABLE [dbo].[MenuItem_Modifiers] (
                        [MenuItemId] INT NOT NULL,
                        [ModifierId] INT NOT NULL,
                        PRIMARY KEY ([MenuItemId], [ModifierId]),
                        CONSTRAINT [FK_MenuItem_Modifiers_MenuItems] FOREIGN KEY ([MenuItemId]) REFERENCES [MenuItems]([Id]),
                        CONSTRAINT [FK_MenuItem_Modifiers_Modifiers] FOREIGN KEY ([ModifierId]) REFERENCES [Modifiers]([Id])
                    );
                END
            ");

            // Check if Modifiers table exists
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Modifiers')
                BEGIN
                    CREATE TABLE [dbo].[Modifiers] (
                        [Id] INT IDENTITY(1,1) PRIMARY KEY,
                        [Name] NVARCHAR(100) NOT NULL,
                        [Price] DECIMAL(10, 2) NOT NULL DEFAULT 0,
                        [IsDefault] BIT NOT NULL DEFAULT 0,
                        [CreatedAt] DATETIME NOT NULL DEFAULT GETDATE(),
                        [UpdatedAt] DATETIME NOT NULL DEFAULT GETDATE()
                    );
                END
            ");

            // Check if OrderItemModifiers table exists
            migrationBuilder.Sql(@"
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
                END
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.tables WHERE name = 'MenuItem_Modifiers')
                BEGIN
                    DROP TABLE [dbo].[MenuItem_Modifiers];
                END
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.tables WHERE name = 'OrderItemModifiers')
                BEGIN
                    DROP TABLE [dbo].[OrderItemModifiers];
                END
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Modifiers')
                BEGIN
                    DROP TABLE [dbo].[Modifiers];
                END
            ");
        }
    }
}
