using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FutureReady.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEvaluationAndWorkHistoryModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LogbookEvaluations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlacementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttendancePunctuality = table.Column<int>(type: "int", nullable: false),
                    Appearance = table.Column<int>(type: "int", nullable: false),
                    CommunicationSkills = table.Column<int>(type: "int", nullable: false),
                    Initiative = table.Column<int>(type: "int", nullable: false),
                    WorkQuality = table.Column<int>(type: "int", nullable: false),
                    Teamwork = table.Column<int>(type: "int", nullable: false),
                    SafetyAwareness = table.Column<int>(type: "int", nullable: false),
                    OverallPerformance = table.Column<int>(type: "int", nullable: false),
                    SupervisorName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Comments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SupervisorSignedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
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
                    table.PrimaryKey("PK_LogbookEvaluations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LogbookEvaluations_Placements_PlacementId",
                        column: x => x.PlacementId,
                        principalTable: "Placements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StudentWorkHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrentCourses = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VetQualifications = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Certificates = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PartTimeEmployment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CommunityService = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_StudentWorkHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentWorkHistories_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LogbookEvaluations_PlacementId",
                table: "LogbookEvaluations",
                column: "PlacementId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentWorkHistories_StudentId",
                table: "StudentWorkHistories",
                column: "StudentId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LogbookEvaluations");

            migrationBuilder.DropTable(
                name: "StudentWorkHistories");
        }
    }
}
