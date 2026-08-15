using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SPIP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAIChatFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop the previous AIChatMessages and AIChatSessions tables if they already exist.
            // This keeps the migration idempotent for databases where the AI chat tables were never created.
            migrationBuilder.Sql(
                "IF OBJECT_ID(N'[dbo].[AIChatMessages]', N'U') IS NOT NULL DROP TABLE [dbo].[AIChatMessages];");
            migrationBuilder.Sql(
                "IF OBJECT_ID(N'[dbo].[AIChatSessions]', N'U') IS NOT NULL DROP TABLE [dbo].[AIChatSessions];");

            // ── AIChatSessions — correct schema with uniqueidentifier PK ──────────
            migrationBuilder.CreateTable(
                name: "AIChatSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false,
                        defaultValueSql: "NEWSEQUENTIALID()"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIChatSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AIChatSessions_Users_Domain_UserId",
                        column: x => x.UserId,
                        principalTable: "Users_Domain",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            // ── AIChatMessages — correct schema with uniqueidentifier PK and FK ──
            migrationBuilder.CreateTable(
                name: "AIChatMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false,
                        defaultValueSql: "NEWSEQUENTIALID()"),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TokensUsed = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIChatMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AIChatMessages_AIChatSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "AIChatSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // ── Indexes ────────────────────────────────────────────────────────────

            // Single-column: filter sessions by user
            migrationBuilder.CreateIndex(
                name: "IX_AIChatSessions_UserId",
                table: "AIChatSessions",
                column: "UserId");

            // Composite: ordered session list per user (ORDER BY UpdatedAt DESC)
            migrationBuilder.CreateIndex(
                name: "IX_AIChatSessions_UserId_UpdatedAt",
                table: "AIChatSessions",
                columns: new[] { "UserId", "UpdatedAt" });

            // Composite: message history for a session in chronological order
            migrationBuilder.CreateIndex(
                name: "IX_AIChatMessages_SessionId_CreatedAt",
                table: "AIChatMessages",
                columns: new[] { "SessionId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "AIChatMessages");
            migrationBuilder.DropTable(name: "AIChatSessions");
        }
    }
}
