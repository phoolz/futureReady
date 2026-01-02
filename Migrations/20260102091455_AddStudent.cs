using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FutureReady.Migrations
{
    /// <inheritdoc />
    public partial class AddStudent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CohortTeachers_SchoolCohorts_SchoolCohortId",
                table: "CohortTeachers");

            migrationBuilder.RenameColumn(
                name: "SchoolCohortId",
                table: "CohortTeachers",
                newName: "CohortId");

            migrationBuilder.RenameIndex(
                name: "IX_CohortTeachers_SchoolCohortId",
                table: "CohortTeachers",
                newName: "IX_CohortTeachers_CohortId");

            migrationBuilder.CreateTable(
                name: "Students",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CohortId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MedicareNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StudentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Students", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Students_SchoolCohorts_CohortId",
                        column: x => x.CohortId,
                        principalTable: "SchoolCohorts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Students_CohortId",
                table: "Students",
                column: "CohortId");

            migrationBuilder.AddForeignKey(
                name: "FK_CohortTeachers_SchoolCohorts_CohortId",
                table: "CohortTeachers",
                column: "CohortId",
                principalTable: "SchoolCohorts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CohortTeachers_SchoolCohorts_CohortId",
                table: "CohortTeachers");

            migrationBuilder.DropTable(
                name: "Students");

            migrationBuilder.RenameColumn(
                name: "CohortId",
                table: "CohortTeachers",
                newName: "SchoolCohortId");

            migrationBuilder.RenameIndex(
                name: "IX_CohortTeachers_CohortId",
                table: "CohortTeachers",
                newName: "IX_CohortTeachers_SchoolCohortId");

            migrationBuilder.AddForeignKey(
                name: "FK_CohortTeachers_SchoolCohorts_SchoolCohortId",
                table: "CohortTeachers",
                column: "SchoolCohortId",
                principalTable: "SchoolCohorts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
