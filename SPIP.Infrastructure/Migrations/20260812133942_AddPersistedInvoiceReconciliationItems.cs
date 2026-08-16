using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SPIP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPersistedInvoiceReconciliationItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PurchaseOrderItemId",
                table: "Discrepancies",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReconciliationItemId",
                table: "Discrepancies",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "InvoiceReconciliationItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvoiceId = table.Column<int>(type: "int", nullable: false),
                    PurchaseOrderItemId = table.Column<int>(type: "int", nullable: true),
                    InvoiceItemId = table.Column<int>(type: "int", nullable: true),
                    PurchaseOrderSku = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    InvoiceSku = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ProductName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ExpectedQuantity = table.Column<int>(type: "int", nullable: true),
                    ActualQuantity = table.Column<int>(type: "int", nullable: true),
                    ExpectedUnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ActualUnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ExpectedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ActualAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceReconciliationItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoiceReconciliationItems_InvoiceItems_InvoiceItemId",
                        column: x => x.InvoiceItemId,
                        principalTable: "InvoiceItems",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InvoiceReconciliationItems_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InvoiceReconciliationItems_PurchaseOrderItems_PurchaseOrderItemId",
                        column: x => x.PurchaseOrderItemId,
                        principalTable: "PurchaseOrderItems",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Discrepancies_PurchaseOrderItemId",
                table: "Discrepancies",
                column: "PurchaseOrderItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Discrepancies_ReconciliationItemId",
                table: "Discrepancies",
                column: "ReconciliationItemId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceReconciliationItems_InvoiceId",
                table: "InvoiceReconciliationItems",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceReconciliationItems_InvoiceItemId",
                table: "InvoiceReconciliationItems",
                column: "InvoiceItemId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceReconciliationItems_PurchaseOrderItemId",
                table: "InvoiceReconciliationItems",
                column: "PurchaseOrderItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_Discrepancies_InvoiceReconciliationItems_ReconciliationItemId",
                table: "Discrepancies",
                column: "ReconciliationItemId",
                principalTable: "InvoiceReconciliationItems",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Discrepancies_PurchaseOrderItems_PurchaseOrderItemId",
                table: "Discrepancies",
                column: "PurchaseOrderItemId",
                principalTable: "PurchaseOrderItems",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Discrepancies_InvoiceReconciliationItems_ReconciliationItemId",
                table: "Discrepancies");

            migrationBuilder.DropForeignKey(
                name: "FK_Discrepancies_PurchaseOrderItems_PurchaseOrderItemId",
                table: "Discrepancies");

            migrationBuilder.DropTable(
                name: "InvoiceReconciliationItems");

            migrationBuilder.DropIndex(
                name: "IX_Discrepancies_PurchaseOrderItemId",
                table: "Discrepancies");

            migrationBuilder.DropIndex(
                name: "IX_Discrepancies_ReconciliationItemId",
                table: "Discrepancies");

            migrationBuilder.DropColumn(
                name: "PurchaseOrderItemId",
                table: "Discrepancies");

            migrationBuilder.DropColumn(
                name: "ReconciliationItemId",
                table: "Discrepancies");
        }
    }
}
