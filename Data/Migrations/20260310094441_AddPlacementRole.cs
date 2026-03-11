using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Apiary.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPlacementRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PlacementRole",
                table: "Placements",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PlacementRole",
                table: "Placements");
        }
    }
}
