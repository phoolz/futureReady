using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Apiary.Data.Migrations
{
    /// <inheritdoc />
    public partial class MultiStudentPlacements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Drop existing foreign keys on logbook tables
            migrationBuilder.DropForeignKey(
                name: "FK_LogbookEntries_Placements_PlacementId",
                table: "LogbookEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_LogbookEvaluations_Placements_PlacementId",
                table: "LogbookEvaluations");

            migrationBuilder.DropForeignKey(
                name: "FK_LogbookTasks_Placements_PlacementId",
                table: "LogbookTasks");

            migrationBuilder.DropIndex(
                name: "IX_ParentPermissions_PlacementId",
                table: "ParentPermissions");

            // Step 2: Add StudentId to ParentPermissions and FormTokens
            migrationBuilder.AddColumn<Guid>(
                name: "StudentId",
                table: "ParentPermissions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "StudentId",
                table: "FormTokens",
                type: "uniqueidentifier",
                nullable: true);

            // Step 3: Create PlacementStudents table
            migrationBuilder.CreateTable(
                name: "PlacementStudents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlacementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ParentSubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_PlacementStudents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlacementStudents_Placements_PlacementId",
                        column: x => x.PlacementId,
                        principalTable: "Placements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlacementStudents_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlacementStudents_PlacementId_StudentId",
                table: "PlacementStudents",
                columns: new[] { "PlacementId", "StudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlacementStudents_StudentId",
                table: "PlacementStudents",
                column: "StudentId");

            // Step 4: Migrate existing placement-student relationships to PlacementStudents table
            // Create a PlacementStudent record for each Placement that has a StudentId
            migrationBuilder.Sql(@"
                INSERT INTO PlacementStudents (Id, PlacementId, StudentId, Status, ParentSubmittedAt, CreatedBy, CreatedAt, UpdatedBy, UpdatedAt, IsDeleted, DeletedBy, DeletedAt, TenantId)
                SELECT
                    NEWID() as Id,
                    p.Id as PlacementId,
                    p.StudentId,
                    CASE
                        WHEN p.ParentSubmittedAt IS NOT NULL THEN 'confirmed'
                        ELSE 'pending_parent'
                    END as Status,
                    p.ParentSubmittedAt,
                    p.CreatedBy,
                    p.CreatedAt,
                    p.UpdatedBy,
                    p.UpdatedAt,
                    p.IsDeleted,
                    p.DeletedBy,
                    p.DeletedAt,
                    p.TenantId
                FROM Placements p
                WHERE p.StudentId IS NOT NULL AND p.StudentId != '00000000-0000-0000-0000-000000000000'
            ");

            // Step 5: Add PlacementStudentId column to logbook tables (nullable initially)
            migrationBuilder.AddColumn<Guid>(
                name: "PlacementStudentId",
                table: "LogbookEntries",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PlacementStudentId",
                table: "LogbookTasks",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PlacementStudentId",
                table: "LogbookEvaluations",
                type: "uniqueidentifier",
                nullable: true);

            // Step 6: Update logbook tables to reference PlacementStudents instead of Placements
            migrationBuilder.Sql(@"
                UPDATE le
                SET le.PlacementStudentId = ps.Id
                FROM LogbookEntries le
                INNER JOIN PlacementStudents ps ON ps.PlacementId = le.PlacementId
            ");

            migrationBuilder.Sql(@"
                UPDATE lt
                SET lt.PlacementStudentId = ps.Id
                FROM LogbookTasks lt
                INNER JOIN PlacementStudents ps ON ps.PlacementId = lt.PlacementId
            ");

            migrationBuilder.Sql(@"
                UPDATE le
                SET le.PlacementStudentId = ps.Id
                FROM LogbookEvaluations le
                INNER JOIN PlacementStudents ps ON ps.PlacementId = le.PlacementId
            ");

            // Step 7: Update ParentPermissions to set StudentId from Placements
            migrationBuilder.Sql(@"
                UPDATE pp
                SET pp.StudentId = p.StudentId
                FROM ParentPermissions pp
                INNER JOIN Placements p ON p.Id = pp.PlacementId
                WHERE p.StudentId IS NOT NULL AND p.StudentId != '00000000-0000-0000-0000-000000000000'
            ");

            // Step 8: Update FormTokens to set StudentId from Placements (for parent form tokens)
            migrationBuilder.Sql(@"
                UPDATE ft
                SET ft.StudentId = p.StudentId
                FROM FormTokens ft
                INNER JOIN Placements p ON p.Id = ft.PlacementId
                WHERE ft.FormType = 'parent' AND p.StudentId IS NOT NULL AND p.StudentId != '00000000-0000-0000-0000-000000000000'
            ");

            // Step 9: Drop old PlacementId columns from logbook tables
            migrationBuilder.DropIndex(
                name: "IX_LogbookTasks_PlacementId_DatePerformed",
                table: "LogbookTasks");

            migrationBuilder.DropIndex(
                name: "IX_LogbookEvaluations_PlacementId",
                table: "LogbookEvaluations");

            migrationBuilder.DropIndex(
                name: "IX_LogbookEntries_PlacementId_Date",
                table: "LogbookEntries");

            migrationBuilder.DropColumn(
                name: "PlacementId",
                table: "LogbookEntries");

            migrationBuilder.DropColumn(
                name: "PlacementId",
                table: "LogbookTasks");

            migrationBuilder.DropColumn(
                name: "PlacementId",
                table: "LogbookEvaluations");

            // Step 10: Make PlacementStudentId columns non-nullable and add indexes
            migrationBuilder.AlterColumn<Guid>(
                name: "PlacementStudentId",
                table: "LogbookEntries",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "PlacementStudentId",
                table: "LogbookTasks",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "PlacementStudentId",
                table: "LogbookEvaluations",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_LogbookEntries_PlacementStudentId_Date",
                table: "LogbookEntries",
                columns: new[] { "PlacementStudentId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LogbookTasks_PlacementStudentId_DatePerformed",
                table: "LogbookTasks",
                columns: new[] { "PlacementStudentId", "DatePerformed" });

            migrationBuilder.CreateIndex(
                name: "IX_LogbookEvaluations_PlacementStudentId",
                table: "LogbookEvaluations",
                column: "PlacementStudentId");

            // Step 11: Drop StudentId and ParentSubmittedAt from Placements
            migrationBuilder.DropForeignKey(
                name: "FK_Placements_Students_StudentId",
                table: "Placements");

            migrationBuilder.DropIndex(
                name: "IX_Placements_StudentId",
                table: "Placements");

            migrationBuilder.DropColumn(
                name: "ParentSubmittedAt",
                table: "Placements");

            migrationBuilder.DropColumn(
                name: "StudentId",
                table: "Placements");

            // Step 12: Add remaining indexes and foreign keys
            migrationBuilder.CreateIndex(
                name: "IX_ParentPermissions_PlacementId_StudentId",
                table: "ParentPermissions",
                columns: new[] { "PlacementId", "StudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ParentPermissions_StudentId",
                table: "ParentPermissions",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_FormTokens_StudentId",
                table: "FormTokens",
                column: "StudentId");

            migrationBuilder.AddForeignKey(
                name: "FK_FormTokens_Students_StudentId",
                table: "FormTokens",
                column: "StudentId",
                principalTable: "Students",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LogbookEntries_PlacementStudents_PlacementStudentId",
                table: "LogbookEntries",
                column: "PlacementStudentId",
                principalTable: "PlacementStudents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LogbookEvaluations_PlacementStudents_PlacementStudentId",
                table: "LogbookEvaluations",
                column: "PlacementStudentId",
                principalTable: "PlacementStudents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LogbookTasks_PlacementStudents_PlacementStudentId",
                table: "LogbookTasks",
                column: "PlacementStudentId",
                principalTable: "PlacementStudents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ParentPermissions_Students_StudentId",
                table: "ParentPermissions",
                column: "StudentId",
                principalTable: "Students",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FormTokens_Students_StudentId",
                table: "FormTokens");

            migrationBuilder.DropForeignKey(
                name: "FK_LogbookEntries_PlacementStudents_PlacementStudentId",
                table: "LogbookEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_LogbookEvaluations_PlacementStudents_PlacementStudentId",
                table: "LogbookEvaluations");

            migrationBuilder.DropForeignKey(
                name: "FK_LogbookTasks_PlacementStudents_PlacementStudentId",
                table: "LogbookTasks");

            migrationBuilder.DropForeignKey(
                name: "FK_ParentPermissions_Students_StudentId",
                table: "ParentPermissions");

            migrationBuilder.DropTable(
                name: "PlacementStudents");

            migrationBuilder.DropIndex(
                name: "IX_ParentPermissions_PlacementId_StudentId",
                table: "ParentPermissions");

            migrationBuilder.DropIndex(
                name: "IX_ParentPermissions_StudentId",
                table: "ParentPermissions");

            migrationBuilder.DropIndex(
                name: "IX_FormTokens_StudentId",
                table: "FormTokens");

            migrationBuilder.DropColumn(
                name: "StudentId",
                table: "ParentPermissions");

            migrationBuilder.DropColumn(
                name: "StudentId",
                table: "FormTokens");

            migrationBuilder.RenameColumn(
                name: "PlacementStudentId",
                table: "LogbookTasks",
                newName: "PlacementId");

            migrationBuilder.RenameIndex(
                name: "IX_LogbookTasks_PlacementStudentId_DatePerformed",
                table: "LogbookTasks",
                newName: "IX_LogbookTasks_PlacementId_DatePerformed");

            migrationBuilder.RenameColumn(
                name: "PlacementStudentId",
                table: "LogbookEvaluations",
                newName: "PlacementId");

            migrationBuilder.RenameIndex(
                name: "IX_LogbookEvaluations_PlacementStudentId",
                table: "LogbookEvaluations",
                newName: "IX_LogbookEvaluations_PlacementId");

            migrationBuilder.RenameColumn(
                name: "PlacementStudentId",
                table: "LogbookEntries",
                newName: "PlacementId");

            migrationBuilder.RenameIndex(
                name: "IX_LogbookEntries_PlacementStudentId_Date",
                table: "LogbookEntries",
                newName: "IX_LogbookEntries_PlacementId_Date");

            migrationBuilder.AddColumn<DateTime>(
                name: "ParentSubmittedAt",
                table: "Placements",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "StudentId",
                table: "Placements",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Placements_StudentId",
                table: "Placements",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_ParentPermissions_PlacementId",
                table: "ParentPermissions",
                column: "PlacementId");

            migrationBuilder.AddForeignKey(
                name: "FK_LogbookEntries_Placements_PlacementId",
                table: "LogbookEntries",
                column: "PlacementId",
                principalTable: "Placements",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LogbookEvaluations_Placements_PlacementId",
                table: "LogbookEvaluations",
                column: "PlacementId",
                principalTable: "Placements",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LogbookTasks_Placements_PlacementId",
                table: "LogbookTasks",
                column: "PlacementId",
                principalTable: "Placements",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Placements_Students_StudentId",
                table: "Placements",
                column: "StudentId",
                principalTable: "Students",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
