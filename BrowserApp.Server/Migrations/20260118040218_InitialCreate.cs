using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrowserApp.Server.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Username = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MarketplaceRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Site = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    RulesJson = table.Column<string>(type: "jsonb", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                    DownloadCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    Tags = table.Column<string[]>(type: "text[]", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketplaceRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarketplaceRules_Users_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MarketplaceRules_AuthorId",
                table: "MarketplaceRules",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketplaceRules_CreatedAt",
                table: "MarketplaceRules",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_MarketplaceRules_DownloadCount",
                table: "MarketplaceRules",
                column: "DownloadCount");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MarketplaceRules");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
