using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrowserApp.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddChannelAuditLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChannelAuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChannelId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Metadata = table.Column<string>(type: "jsonb", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChannelAuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChannelAuditLogs_Channels_ChannelId",
                        column: x => x.ChannelId,
                        principalTable: "Channels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChannelAuditLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChannelAuditLogs_ChannelId",
                table: "ChannelAuditLogs",
                column: "ChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelAuditLogs_Timestamp",
                table: "ChannelAuditLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelAuditLogs_UserId",
                table: "ChannelAuditLogs",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChannelAuditLogs");
        }
    }
}
