using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Apiary.Data.Migrations
{
    /// <inheritdoc />
    public partial class DropLogbookEntryCumulativeHours : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CumulativeHours",
                table: "LogbookEntries");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
