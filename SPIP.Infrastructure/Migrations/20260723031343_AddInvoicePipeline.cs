using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SPIP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoicePipeline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_PurchaseOrders_PurchaseOrderId",
                table: "Invoices");

            migrationBuilder.AlterColumn<string>(
                name: "FileName",
                table: "UploadedFiles",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "ContentType",
                table: "UploadedFiles",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OriginalFileName",
                table: "UploadedFiles",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StoredFileName",
                table: "UploadedFiles",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<int>(
                name: "PurchaseOrderId",
                table: "Invoices",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "InvoiceNumber",
                table: "Invoices",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "Invoices",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "USD");

            migrationBuilder.AddColumn<decimal>(
                name: "Subtotal",
                table: "Invoices",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "UploadedByUserId",
                table: "Invoices",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "Vat",
                table: "Invoices",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "VendorName",
                table: "Invoices",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SupplierSku",
                table: "InvoiceItems",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "FieldName",
                table: "Discrepancies",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "ExpectedValue",
                table: "Discrepancies",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "ActualValue",
                table: "Discrepancies",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<int>(
                name: "DiscrepancyType",
                table: "Discrepancies",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "InvoiceItemId",
                table: "Discrepancies",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "InvoiceProcessingLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvoiceId = table.Column<int>(type: "int", nullable: false),
                    FromStatus = table.Column<int>(type: "int", nullable: true),
                    ToStatus = table.Column<int>(type: "int", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceProcessingLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoiceProcessingLogs_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "PermissionCatalogs",
                keyColumn: "Id",
                keyValue: 20,
                columns: new[] { "DisplayName", "Module", "SystemName" },
                values: new object[] { "Upload Invoices", "Invoices", "Invoices.Upload" });

            migrationBuilder.UpdateData(
                table: "PermissionCatalogs",
                keyColumn: "Id",
                keyValue: 21,
                columns: new[] { "DisplayName", "Module", "SystemName" },
                values: new object[] { "View Invoices", "Invoices", "Invoices.View" });

            migrationBuilder.UpdateData(
                table: "PermissionCatalogs",
                keyColumn: "Id",
                keyValue: 22,
                columns: new[] { "DisplayName", "Module", "SystemName" },
                values: new object[] { "Download Invoices", "Invoices", "Invoices.Download" });

            migrationBuilder.UpdateData(
                table: "PermissionCatalogs",
                keyColumn: "Id",
                keyValue: 23,
                columns: new[] { "DisplayName", "Module", "SystemName" },
                values: new object[] { "ViewAll Invoices", "Invoices", "Invoices.ViewAll" });

            migrationBuilder.InsertData(
                table: "PermissionCatalogs",
                columns: new[] { "Id", "DisplayName", "Module", "SystemName" },
                values: new object[,]
                {
                    { 24, "View VendorMappings", "VendorMappings", "VendorMappings.View" },
                    { 25, "Manage VendorMappings", "VendorMappings", "VendorMappings.Manage" },
                    { 26, "View Dashboard", "Dashboard", "Dashboard.View" },
                    { 27, "View Reports", "Reports", "Reports.View" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_Status",
                table: "Invoices",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_UploadedByUserId",
                table: "Invoices",
                column: "UploadedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Discrepancies_InvoiceItemId",
                table: "Discrepancies",
                column: "InvoiceItemId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceProcessingLogs_InvoiceId_CreatedAt",
                table: "InvoiceProcessingLogs",
                columns: new[] { "InvoiceId", "CreatedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_Discrepancies_InvoiceItems_InvoiceItemId",
                table: "Discrepancies",
                column: "InvoiceItemId",
                principalTable: "InvoiceItems",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_PurchaseOrders_PurchaseOrderId",
                table: "Invoices",
                column: "PurchaseOrderId",
                principalTable: "PurchaseOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Users_Domain_UploadedByUserId",
                table: "Invoices",
                column: "UploadedByUserId",
                principalTable: "Users_Domain",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Discrepancies_InvoiceItems_InvoiceItemId",
                table: "Discrepancies");

            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_PurchaseOrders_PurchaseOrderId",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Users_Domain_UploadedByUserId",
                table: "Invoices");

            migrationBuilder.DropTable(
                name: "InvoiceProcessingLogs");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_Status",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_UploadedByUserId",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Discrepancies_InvoiceItemId",
                table: "Discrepancies");

            migrationBuilder.DeleteData(
                table: "PermissionCatalogs",
                keyColumn: "Id",
                keyValue: 24);

            migrationBuilder.DeleteData(
                table: "PermissionCatalogs",
                keyColumn: "Id",
                keyValue: 25);

            migrationBuilder.DeleteData(
                table: "PermissionCatalogs",
                keyColumn: "Id",
                keyValue: 26);

            migrationBuilder.DeleteData(
                table: "PermissionCatalogs",
                keyColumn: "Id",
                keyValue: 27);

            migrationBuilder.DropColumn(
                name: "OriginalFileName",
                table: "UploadedFiles");

            migrationBuilder.DropColumn(
                name: "StoredFileName",
                table: "UploadedFiles");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "Subtotal",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "UploadedByUserId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "Vat",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "VendorName",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "SupplierSku",
                table: "InvoiceItems");

            migrationBuilder.DropColumn(
                name: "DiscrepancyType",
                table: "Discrepancies");

            migrationBuilder.DropColumn(
                name: "InvoiceItemId",
                table: "Discrepancies");

            migrationBuilder.AlterColumn<string>(
                name: "FileName",
                table: "UploadedFiles",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "ContentType",
                table: "UploadedFiles",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "PurchaseOrderId",
                table: "Invoices",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "InvoiceNumber",
                table: "Invoices",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "FieldName",
                table: "Discrepancies",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "ExpectedValue",
                table: "Discrepancies",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "ActualValue",
                table: "Discrepancies",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.UpdateData(
                table: "PermissionCatalogs",
                keyColumn: "Id",
                keyValue: 20,
                columns: new[] { "DisplayName", "Module", "SystemName" },
                values: new object[] { "View VendorMappings", "VendorMappings", "VendorMappings.View" });

            migrationBuilder.UpdateData(
                table: "PermissionCatalogs",
                keyColumn: "Id",
                keyValue: 21,
                columns: new[] { "DisplayName", "Module", "SystemName" },
                values: new object[] { "Manage VendorMappings", "VendorMappings", "VendorMappings.Manage" });

            migrationBuilder.UpdateData(
                table: "PermissionCatalogs",
                keyColumn: "Id",
                keyValue: 22,
                columns: new[] { "DisplayName", "Module", "SystemName" },
                values: new object[] { "View Dashboard", "Dashboard", "Dashboard.View" });

            migrationBuilder.UpdateData(
                table: "PermissionCatalogs",
                keyColumn: "Id",
                keyValue: 23,
                columns: new[] { "DisplayName", "Module", "SystemName" },
                values: new object[] { "View Reports", "Reports", "Reports.View" });

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_PurchaseOrders_PurchaseOrderId",
                table: "Invoices",
                column: "PurchaseOrderId",
                principalTable: "PurchaseOrders",
                principalColumn: "Id");
        }
    }
}
