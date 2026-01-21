using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FutureReady.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSchoolContactFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContactEmail",
                table: "Schools",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactPhone",
                table: "Schools",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContactEmail",
                table: "Schools");

            migrationBuilder.DropColumn(
                name: "ContactPhone",
                table: "Schools");
        }
    }
}
