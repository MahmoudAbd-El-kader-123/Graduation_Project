using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SPIP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateUserEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PasswordHash",
                table: "Users_Domain");

            migrationBuilder.AddColumn<Guid>(
                name: "IdentityId",
                table: "Users_Domain",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IdentityId",
                table: "Users_Domain");

            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                table: "Users_Domain",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
