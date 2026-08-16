using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SPIP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePOItemsForVAT : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Sku",
                table: "Products",
                newName: "SkuSupplier");

            migrationBuilder.AddColumn<decimal>(
                name: "Amount",
                table: "PurchaseOrderItems",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "VatAmount",
                table: "PurchaseOrderItems",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "VatPercentage",
                table: "PurchaseOrderItems",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "SkuRetailer",
                table: "Products",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Amount",
                table: "PurchaseOrderItems");

            migrationBuilder.DropColumn(
                name: "VatAmount",
                table: "PurchaseOrderItems");

            migrationBuilder.DropColumn(
                name: "VatPercentage",
                table: "PurchaseOrderItems");

            migrationBuilder.DropColumn(
                name: "SkuRetailer",
                table: "Products");

            migrationBuilder.RenameColumn(
                name: "SkuSupplier",
                table: "Products",
                newName: "Sku");
        }
    }
}
