using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrowserApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddChannelMemberships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChannelMemberships",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ChannelId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ChannelName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ChannelDescription = table.Column<string>(type: "TEXT", nullable: false),
                    Username = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    JoinedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastSyncedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RuleCount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChannelMemberships", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChannelMemberships_ChannelId",
                table: "ChannelMemberships",
                column: "ChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelMemberships_IsActive",
                table: "ChannelMemberships",
                column: "IsActive");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChannelMemberships");
        }
    }
}
