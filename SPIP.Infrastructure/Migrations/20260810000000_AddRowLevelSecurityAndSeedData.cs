using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SPIP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRowLevelSecurityAndSeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Create Security Schema if not exists
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'Security')
                BEGIN
                    EXEC('CREATE SCHEMA Security');
                END;
            ");

            // 2. Create Invoice Security Predicate Function (System-compatible)
            migrationBuilder.Sql(@"
                CREATE OR ALTER FUNCTION Security.fn_invoiceSecurityPredicate(@UploadedByUserId INT)
                RETURNS TABLE WITH SCHEMABINDING AS
                RETURN SELECT 1 AS result
                WHERE 
                    @UploadedByUserId = CAST(SESSION_CONTEXT(N'UserId') AS INT)
                    OR CAST(SESSION_CONTEXT(N'IsAdmin') AS INT) = 1
                    OR SESSION_CONTEXT(N'UserId') IS NULL;
            ");

            // 3. Create PO Security Predicate Function (System-compatible)
            migrationBuilder.Sql(@"
                CREATE OR ALTER FUNCTION Security.fn_poSecurityPredicate(@RequestedByUserId INT)
                RETURNS TABLE WITH SCHEMABINDING AS
                RETURN SELECT 1 AS result
                WHERE 
                    @RequestedByUserId = CAST(SESSION_CONTEXT(N'UserId') AS INT)
                    OR CAST(SESSION_CONTEXT(N'IsAdmin') AS INT) = 1
                    OR SESSION_CONTEXT(N'UserId') IS NULL;
            ");

            // 4. Create Invoice Security Policy
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.security_policies WHERE name = 'InvoiceSecurityPolicy')
                    DROP SECURITY POLICY Security.InvoiceSecurityPolicy;

                CREATE SECURITY POLICY Security.InvoiceSecurityPolicy
                ADD FILTER PREDICATE Security.fn_invoiceSecurityPredicate(UploadedByUserId)
                ON dbo.Invoices
                WITH (STATE = ON);
            ");

            // 5. Create Purchase Order Security Policy
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.security_policies WHERE name = 'POSecurityPolicy')
                    DROP SECURITY POLICY Security.POSecurityPolicy;

                CREATE SECURITY POLICY Security.POSecurityPolicy
                ADD FILTER PREDICATE Security.fn_poSecurityPredicate(RequestedByUserId)
                ON dbo.PurchaseOrders
                WITH (STATE = ON);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF EXISTS (SELECT * FROM sys.security_policies WHERE name = 'InvoiceSecurityPolicy') DROP SECURITY POLICY Security.InvoiceSecurityPolicy;");
            migrationBuilder.Sql("IF EXISTS (SELECT * FROM sys.security_policies WHERE name = 'POSecurityPolicy') DROP SECURITY POLICY Security.POSecurityPolicy;");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS Security.fn_invoiceSecurityPredicate;");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS Security.fn_poSecurityPredicate;");
        }
    }
}
