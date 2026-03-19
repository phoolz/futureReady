using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Apiary.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPlacementDates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "FinishDate",
                table: "Placements",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "StartDate",
                table: "Placements",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FinishDate",
                table: "Placements");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "Placements");
        }
    }
}
