using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SPIP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixUserRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Role",
                table: "Users_Domain");

            migrationBuilder.AddColumn<Guid>(
                name: "RoleId",
                table: "Users_Domain",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "RoleName",
                table: "Users_Domain",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RoleId",
                table: "Users_Domain");

            migrationBuilder.DropColumn(
                name: "RoleName",
                table: "Users_Domain");

            migrationBuilder.AddColumn<int>(
                name: "Role",
                table: "Users_Domain",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
