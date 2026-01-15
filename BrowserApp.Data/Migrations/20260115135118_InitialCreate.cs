using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrowserApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BrowsingHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Url = table.Column<string>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: true),
                    VisitedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BrowsingHistory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NetworkLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Url = table.Column<string>(type: "TEXT", nullable: false),
                    Method = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "GET"),
                    StatusCode = table.Column<int>(type: "INTEGER", nullable: true),
                    ResourceType = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "Unknown"),
                    ContentType = table.Column<string>(type: "TEXT", nullable: true),
                    Size = table.Column<long>(type: "INTEGER", nullable: true),
                    WasBlocked = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    BlockedByRuleId = table.Column<string>(type: "TEXT", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NetworkLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Rules",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Site = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Enabled = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 10),
                    RulesJson = table.Column<string>(type: "TEXT", nullable: false),
                    Source = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false, defaultValue: "local"),
                    ChannelId = table.Column<string>(type: "TEXT", nullable: true),
                    IsEnforced = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Settings",
                columns: table => new
                {
                    Key = table.Column<string>(type: "TEXT", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settings", x => x.Key);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BrowsingHistory_VisitedAt",
                table: "BrowsingHistory",
                column: "VisitedAt");

            migrationBuilder.CreateIndex(
                name: "IX_NetworkLogs_ResourceType",
                table: "NetworkLogs",
                column: "ResourceType");

            migrationBuilder.CreateIndex(
                name: "IX_NetworkLogs_Timestamp",
                table: "NetworkLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_NetworkLogs_WasBlocked",
                table: "NetworkLogs",
                column: "WasBlocked");

            migrationBuilder.CreateIndex(
                name: "IX_Rules_Enabled",
                table: "Rules",
                column: "Enabled");

            migrationBuilder.CreateIndex(
                name: "IX_Rules_Priority",
                table: "Rules",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_Rules_Source",
                table: "Rules",
                column: "Source");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BrowsingHistory");

            migrationBuilder.DropTable(
                name: "NetworkLogs");

            migrationBuilder.DropTable(
                name: "Rules");

            migrationBuilder.DropTable(
                name: "Settings");
        }
    }
}
