using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FutureReady.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCohortEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Students_SchoolCohorts_CohortId",
                table: "Students");

            migrationBuilder.DropTable(
                name: "CohortTeachers");

            migrationBuilder.DropTable(
                name: "SchoolCohorts");

            migrationBuilder.DropIndex(
                name: "IX_Students_CohortId",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "CohortId",
                table: "Students");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CohortId",
                table: "Students",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "SchoolCohorts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    GraduationMonth = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    GraduationYear = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchoolCohorts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SchoolCohorts_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CohortTeachers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CohortId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TeacherId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsSubstitute = table.Column<bool>(type: "bit", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Year = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CohortTeachers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CohortTeachers_SchoolCohorts_CohortId",
                        column: x => x.CohortId,
                        principalTable: "SchoolCohorts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CohortTeachers_Teachers_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "Teachers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Students_CohortId",
                table: "Students",
                column: "CohortId");

            migrationBuilder.CreateIndex(
                name: "IX_CohortTeachers_CohortId",
                table: "CohortTeachers",
                column: "CohortId");

            migrationBuilder.CreateIndex(
                name: "IX_CohortTeachers_TeacherId",
                table: "CohortTeachers",
                column: "TeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_SchoolCohorts_SchoolId",
                table: "SchoolCohorts",
                column: "SchoolId");

            migrationBuilder.AddForeignKey(
                name: "FK_Students_SchoolCohorts_CohortId",
                table: "Students",
                column: "CohortId",
                principalTable: "SchoolCohorts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
