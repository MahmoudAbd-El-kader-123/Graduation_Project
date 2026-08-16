using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SPIP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPermissionCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PermissionCatalogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SystemName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Module = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermissionCatalogs", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "PermissionCatalogs",
                columns: new[] { "Id", "DisplayName", "Module", "SystemName" },
                values: new object[,]
                {
                    { 1, "View Users", "Users", "Users.View" },
                    { 2, "Update Users", "Users", "Users.Update" },
                    { 3, "Activate Users", "Users", "Users.Activate" },
                    { 4, "Deactivate Users", "Users", "Users.Deactivate" },
                    { 5, "View Roles", "Roles", "Roles.View" },
                    { 6, "Create Roles", "Roles", "Roles.Create" },
                    { 7, "Update Roles", "Roles", "Roles.Update" },
                    { 8, "Delete Roles", "Roles", "Roles.Delete" },
                    { 9, "View Vendors", "Vendors", "Vendors.View" },
                    { 10, "Create Vendors", "Vendors", "Vendors.Create" },
                    { 11, "Update Vendors", "Vendors", "Vendors.Update" },
                    { 12, "Delete Vendors", "Vendors", "Vendors.Delete" },
                    { 13, "View Products", "Products", "Products.View" },
                    { 14, "Create Products", "Products", "Products.Create" },
                    { 15, "Update Products", "Products", "Products.Update" },
                    { 16, "Delete Products", "Products", "Products.Delete" },
                    { 17, "View POImports", "POImports", "POImports.View" },
                    { 18, "Import POImports", "POImports", "POImports.Import" },
                    { 19, "Delete POImports", "POImports", "POImports.Delete" },
                    { 20, "View VendorMappings", "VendorMappings", "VendorMappings.View" },
                    { 21, "Manage VendorMappings", "VendorMappings", "VendorMappings.Manage" },
                    { 22, "View Dashboard", "Dashboard", "Dashboard.View" },
                    { 23, "View Reports", "Reports", "Reports.View" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PermissionCatalogs");
        }
    }
}
