using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FutureReady.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateStudentFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GuardianEmail",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "GuardianName",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Students");

            migrationBuilder.RenameColumn(
                name: "GuardianPhone",
                table: "Students",
                newName: "YearLevel");

            migrationBuilder.AddColumn<int>(
                name: "GraduationYear",
                table: "Students",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StudentType",
                table: "Students",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GraduationYear",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "StudentType",
                table: "Students");

            migrationBuilder.RenameColumn(
                name: "YearLevel",
                table: "Students",
                newName: "GuardianPhone");

            migrationBuilder.AddColumn<string>(
                name: "GuardianEmail",
                table: "Students",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GuardianName",
                table: "Students",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Students",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
