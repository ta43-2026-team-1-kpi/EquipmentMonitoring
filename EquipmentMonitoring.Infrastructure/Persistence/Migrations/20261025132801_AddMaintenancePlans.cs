using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EquipmentMonitoring.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMaintenancePlans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MaintenancePlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EquipmentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PlannedDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    WorkType = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ResponsibleEmployee = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Note = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenancePlans", x => x.Id);
                    table.CheckConstraint("CK_MaintenancePlans_Note_Length", "Note IS NULL OR length(Note) <= 1000");
                    table.CheckConstraint("CK_MaintenancePlans_ResponsibleEmployee_Length", "length(ResponsibleEmployee) <= 200");
                    table.CheckConstraint("CK_MaintenancePlans_Status_Allowed", "Status IN ('Planned', 'Completed', 'Cancelled')");
                    table.CheckConstraint("CK_MaintenancePlans_WorkType_Length", "length(WorkType) <= 200");
                    table.ForeignKey(
                        name: "FK_MaintenancePlans_Equipment_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MaintenancePlans_EquipmentId_PlannedDate",
                table: "MaintenancePlans",
                columns: new[] { "EquipmentId", "PlannedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_MaintenancePlans_PlannedDate_Id",
                table: "MaintenancePlans",
                columns: new[] { "PlannedDate", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MaintenancePlans");
        }
    }
}
