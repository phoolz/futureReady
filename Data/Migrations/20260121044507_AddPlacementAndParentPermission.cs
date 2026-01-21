using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FutureReady.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPlacementAndParentPermission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Placements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SupervisorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Year = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DressRequirement = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    WorkStartTime = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    WorkEndTime = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    HasOhsPolicy = table.Column<bool>(type: "bit", nullable: true),
                    HasInductionProgram = table.Column<bool>(type: "bit", nullable: true),
                    SafetyBriefingMethod = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    HasObviousHazards = table.Column<bool>(type: "bit", nullable: true),
                    HazardDetails = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InjuryPreventionTraining = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProvidesHazardReportingInstruction = table.Column<bool>(type: "bit", nullable: true),
                    HasEmergencyProcedures = table.Column<bool>(type: "bit", nullable: true),
                    HasFireExtinguishersChecked = table.Column<bool>(type: "bit", nullable: true),
                    HasFirstAidKit = table.Column<bool>(type: "bit", nullable: true),
                    HasSafeAmenities = table.Column<bool>(type: "bit", nullable: true),
                    StaffInformedOfStudent = table.Column<bool>(type: "bit", nullable: true),
                    StaffMeetWorkingWithChildrenRequirements = table.Column<bool>(type: "bit", nullable: true),
                    AdditionalInfoRequired = table.Column<bool>(type: "bit", nullable: true),
                    AdditionalInfoDetails = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EmployerRequiresVehicleTravel = table.Column<bool>(type: "bit", nullable: true),
                    EmployerVehicleDetails = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EmployerDriverExperience = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EmployerLicenceType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    HasChemicalHazards = table.Column<bool>(type: "bit", nullable: false),
                    ChemicalDetails = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HasPlantMachineryHazards = table.Column<bool>(type: "bit", nullable: false),
                    PlantMachineryDetails = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HasBiologicalHazards = table.Column<bool>(type: "bit", nullable: false),
                    BiologicalDetails = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HasErgonomicHazards = table.Column<bool>(type: "bit", nullable: false),
                    ErgonomicDetails = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HazardsAdditionalDetails = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EmployerSubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_Placements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Placements_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Placements_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Placements_Supervisors_SupervisorId",
                        column: x => x.SupervisorId,
                        principalTable: "Supervisors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ParentPermissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlacementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TransportMethod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PublicTransportDetails = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DriverName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DriverContactNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RequestTeacherPrevisit = table.Column<bool>(type: "bit", nullable: false),
                    ShareMedicalWithEmployer = table.Column<bool>(type: "bit", nullable: false),
                    MedicalNotesForEmployer = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ParentFirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ParentLastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ConsentGiven = table.Column<bool>(type: "bit", nullable: false),
                    ConsentDate = table.Column<DateOnly>(type: "date", nullable: true),
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
                    table.PrimaryKey("PK_ParentPermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ParentPermissions_Placements_PlacementId",
                        column: x => x.PlacementId,
                        principalTable: "Placements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ParentPermissions_PlacementId",
                table: "ParentPermissions",
                column: "PlacementId");

            migrationBuilder.CreateIndex(
                name: "IX_Placements_CompanyId",
                table: "Placements",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Placements_StudentId",
                table: "Placements",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_Placements_SupervisorId",
                table: "Placements",
                column: "SupervisorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ParentPermissions");

            migrationBuilder.DropTable(
                name: "Placements");
        }
    }
}
