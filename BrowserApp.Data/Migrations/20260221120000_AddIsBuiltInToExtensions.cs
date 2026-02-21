using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrowserApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIsBuiltInToExtensions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsBuiltIn",
                table: "Extensions",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsBuiltIn",
                table: "Extensions");
        }
    }
}
