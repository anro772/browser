using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrowserApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMarketplaceIdToRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MarketplaceId",
                table: "Rules",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MarketplaceId",
                table: "Rules");
        }
    }
}
