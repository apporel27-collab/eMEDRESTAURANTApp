using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace RestaurantManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class CreateSubCategoriesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPartOfMergedOrder",
                table: "Tables",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MergedTableNames",
                table: "Tables",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "GSTPercentage",
                table: "MenuItems",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsGstApplicable",
                table: "MenuItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "NotAvailable",
                table: "MenuItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "RestaurantSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RestaurantName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StreetAddress = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    City = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    State = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Pincode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Country = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    GSTCode = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Website = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LogoPath = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CurrencySymbol = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DefaultGSTPercentage = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TakeAwayGSTPercentage = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RestaurantSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsSystemRole = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SubCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubCategories_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Salt = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FirstName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsLockedOut = table.Column<bool>(type: "bit", nullable: false),
                    FailedLoginAttempts = table.Column<int>(type: "int", nullable: false),
                    LastLoginDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RequiresMFA = table.Column<bool>(type: "bit", nullable: false),
                    MustChangePassword = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    Password = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ConfirmPassword = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SelectedRoleIds = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Reservations",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "ReservationDate", "ReservationTime", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 10, 5, 10, 55, 40, 331, DateTimeKind.Local).AddTicks(9270), new DateTime(2025, 10, 5, 0, 0, 0, 0, DateTimeKind.Local), new DateTime(2025, 10, 5, 19, 0, 0, 0, DateTimeKind.Local), new DateTime(2025, 10, 5, 10, 55, 40, 331, DateTimeKind.Local).AddTicks(9270) });

            migrationBuilder.UpdateData(
                table: "Reservations",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "ReservationDate", "ReservationTime", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 10, 5, 10, 55, 40, 332, DateTimeKind.Local).AddTicks(20), new DateTime(2025, 10, 6, 0, 0, 0, 0, DateTimeKind.Local), new DateTime(2025, 10, 6, 18, 30, 0, 0, DateTimeKind.Local), new DateTime(2025, 10, 5, 10, 55, 40, 332, DateTimeKind.Local).AddTicks(20) });

            migrationBuilder.InsertData(
                table: "SubCategories",
                columns: new[] { "Id", "CategoryId", "CreatedAt", "Description", "DisplayOrder", "IsActive", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, 1, new DateTime(2025, 10, 5, 10, 55, 40, 331, DateTimeKind.Local).AddTicks(7260), "Warm appetizer dishes", 1, true, "Hot Appetizers", new DateTime(2025, 10, 5, 10, 55, 40, 331, DateTimeKind.Local).AddTicks(7370) },
                    { 2, 1, new DateTime(2025, 10, 5, 10, 55, 40, 331, DateTimeKind.Local).AddTicks(7480), "Cold appetizer dishes", 2, true, "Cold Appetizers", new DateTime(2025, 10, 5, 10, 55, 40, 331, DateTimeKind.Local).AddTicks(7480) },
                    { 3, 2, new DateTime(2025, 10, 5, 10, 55, 40, 331, DateTimeKind.Local).AddTicks(7480), "Meat-based main courses", 1, true, "Meat Dishes", new DateTime(2025, 10, 5, 10, 55, 40, 331, DateTimeKind.Local).AddTicks(7480) },
                    { 4, 2, new DateTime(2025, 10, 5, 10, 55, 40, 331, DateTimeKind.Local).AddTicks(7480), "Vegetarian main courses", 2, true, "Vegetarian Dishes", new DateTime(2025, 10, 5, 10, 55, 40, 331, DateTimeKind.Local).AddTicks(7480) },
                    { 5, 3, new DateTime(2025, 10, 5, 10, 55, 40, 331, DateTimeKind.Local).AddTicks(7490), "Various types of cakes", 1, true, "Cakes", new DateTime(2025, 10, 5, 10, 55, 40, 331, DateTimeKind.Local).AddTicks(7490) },
                    { 6, 3, new DateTime(2025, 10, 5, 10, 55, 40, 331, DateTimeKind.Local).AddTicks(7490), "Ice cream desserts", 2, true, "Ice Cream", new DateTime(2025, 10, 5, 10, 55, 40, 331, DateTimeKind.Local).AddTicks(7490) },
                    { 7, 4, new DateTime(2025, 10, 5, 10, 55, 40, 331, DateTimeKind.Local).AddTicks(7490), "Coffee, tea, hot chocolate", 1, true, "Hot Beverages", new DateTime(2025, 10, 5, 10, 55, 40, 331, DateTimeKind.Local).AddTicks(7490) },
                    { 8, 4, new DateTime(2025, 10, 5, 10, 55, 40, 331, DateTimeKind.Local).AddTicks(7500), "Juices, sodas, iced drinks", 2, true, "Cold Beverages", new DateTime(2025, 10, 5, 10, 55, 40, 331, DateTimeKind.Local).AddTicks(7500) }
                });

            migrationBuilder.UpdateData(
                table: "Tables",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "IsPartOfMergedOrder", "MergedTableNames" },
                values: new object[] { false, null });

            migrationBuilder.UpdateData(
                table: "Tables",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "IsPartOfMergedOrder", "MergedTableNames" },
                values: new object[] { false, null });

            migrationBuilder.UpdateData(
                table: "Tables",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "IsPartOfMergedOrder", "MergedTableNames" },
                values: new object[] { false, null });

            migrationBuilder.UpdateData(
                table: "Tables",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "IsPartOfMergedOrder", "MergedTableNames" },
                values: new object[] { false, null });

            migrationBuilder.CreateIndex(
                name: "IX_SubCategories_CategoryId",
                table: "SubCategories",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleId",
                table: "UserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_UserId",
                table: "UserRoles",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RestaurantSettings");

            migrationBuilder.DropTable(
                name: "SubCategories");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropColumn(
                name: "IsPartOfMergedOrder",
                table: "Tables");

            migrationBuilder.DropColumn(
                name: "MergedTableNames",
                table: "Tables");

            migrationBuilder.DropColumn(
                name: "GSTPercentage",
                table: "MenuItems");

            migrationBuilder.DropColumn(
                name: "IsGstApplicable",
                table: "MenuItems");

            migrationBuilder.DropColumn(
                name: "NotAvailable",
                table: "MenuItems");

            migrationBuilder.UpdateData(
                table: "Reservations",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "ReservationDate", "ReservationTime", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 9, 12, 13, 26, 34, 495, DateTimeKind.Local).AddTicks(6240), new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Local), new DateTime(2025, 9, 12, 19, 0, 0, 0, DateTimeKind.Local), new DateTime(2025, 9, 12, 13, 26, 34, 502, DateTimeKind.Local).AddTicks(8180) });

            migrationBuilder.UpdateData(
                table: "Reservations",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "ReservationDate", "ReservationTime", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 9, 12, 13, 26, 34, 502, DateTimeKind.Local).AddTicks(9220), new DateTime(2025, 9, 13, 0, 0, 0, 0, DateTimeKind.Local), new DateTime(2025, 9, 13, 18, 30, 0, 0, DateTimeKind.Local), new DateTime(2025, 9, 12, 13, 26, 34, 502, DateTimeKind.Local).AddTicks(9220) });
        }
    }
}
