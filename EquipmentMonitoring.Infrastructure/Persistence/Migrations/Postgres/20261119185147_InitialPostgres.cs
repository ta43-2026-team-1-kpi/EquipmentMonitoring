using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EquipmentMonitoring.Infrastructure.Persistence.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class InitialPostgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Equipment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SerialNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Equipment", x => x.Id);
                    table.CheckConstraint("CK_Equipment_Location_Length", "length(\"Location\") <= 200");
                    table.CheckConstraint("CK_Equipment_Name_Length", "length(\"Name\") <= 200");
                    table.CheckConstraint("CK_Equipment_SerialNumber_Length", "length(\"SerialNumber\") <= 100");
                    table.CheckConstraint("CK_Equipment_Status_Allowed", "\"Status\" IN ('Working', 'Faulty', 'UnderMaintenance')");
                    table.CheckConstraint("CK_Equipment_Type_Length", "length(\"Type\") <= 100");
                });

            migrationBuilder.CreateTable(
                name: "MaintenancePlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EquipmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlannedDate = table.Column<DateOnly>(type: "date", nullable: false),
                    WorkType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ResponsibleEmployee = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenancePlans", x => x.Id);
                    table.CheckConstraint("CK_MaintenancePlans_Note_Length", "\"Note\" IS NULL OR length(\"Note\") <= 1000");
                    table.CheckConstraint("CK_MaintenancePlans_ResponsibleEmployee_Length", "length(\"ResponsibleEmployee\") <= 200");
                    table.CheckConstraint("CK_MaintenancePlans_Status_Allowed", "\"Status\" IN ('Planned', 'Completed', 'Cancelled')");
                    table.CheckConstraint("CK_MaintenancePlans_WorkType_Length", "length(\"WorkType\") <= 200");
                    table.ForeignKey(
                        name: "FK_MaintenancePlans_Equipment_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Measurements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EquipmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Value = table.Column<double>(type: "double precision", nullable: false),
                    Unit = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    MeasuredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Measurements", x => x.Id);
                    table.CheckConstraint("CK_Measurements_Type_Allowed", "\"Type\" IN ('Temperature', 'Voltage', 'Load')");
                    table.ForeignKey(
                        name: "FK_Measurements_Equipment_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Thresholds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EquipmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    MeasurementType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    MinValue = table.Column<double>(type: "double precision", nullable: true),
                    MaxValue = table.Column<double>(type: "double precision", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Thresholds", x => x.Id);
                    table.CheckConstraint("CK_Thresholds_AtLeastOneLimit", "\"MinValue\" IS NOT NULL OR \"MaxValue\" IS NOT NULL");
                    table.CheckConstraint("CK_Thresholds_MeasurementType_Allowed", "\"MeasurementType\" IN ('Temperature', 'Voltage', 'Load')");
                    table.CheckConstraint("CK_Thresholds_MinNotGreaterThanMax", "\"MinValue\" IS NULL OR \"MaxValue\" IS NULL OR \"MinValue\" <= \"MaxValue\"");
                    table.ForeignKey(
                        name: "FK_Thresholds_Equipment_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Warnings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EquipmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    MeasurementId = table.Column<Guid>(type: "uuid", nullable: false),
                    ThresholdId = table.Column<Guid>(type: "uuid", nullable: false),
                    ThresholdScope = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    MeasurementType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ActualValue = table.Column<double>(type: "double precision", nullable: false),
                    Unit = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    BreachType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    LimitValue = table.Column<double>(type: "double precision", nullable: false),
                    MeasuredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    AcknowledgedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Warnings", x => x.Id);
                    table.CheckConstraint("CK_Warnings_Acknowledgement_Consistent", "(\"Status\" = 'Active' AND \"AcknowledgedAtUtc\" IS NULL) OR (\"Status\" = 'Acknowledged' AND \"AcknowledgedAtUtc\" IS NOT NULL)");
                    table.CheckConstraint("CK_Warnings_BreachType_Allowed", "\"BreachType\" IN ('BelowMinimum', 'AboveMaximum')");
                    table.CheckConstraint("CK_Warnings_MeasurementType_Allowed", "\"MeasurementType\" IN ('Temperature', 'Voltage', 'Load')");
                    table.CheckConstraint("CK_Warnings_Status_Allowed", "\"Status\" IN ('Active', 'Acknowledged')");
                    table.CheckConstraint("CK_Warnings_ThresholdScope_Allowed", "\"ThresholdScope\" IN ('Global', 'Equipment')");
                    table.ForeignKey(
                        name: "FK_Warnings_Equipment_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Warnings_Measurements_MeasurementId",
                        column: x => x.MeasurementId,
                        principalTable: "Measurements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Equipment_SerialNumber",
                table: "Equipment",
                column: "SerialNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaintenancePlans_EquipmentId_PlannedDate",
                table: "MaintenancePlans",
                columns: new[] { "EquipmentId", "PlannedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_MaintenancePlans_PlannedDate_Id",
                table: "MaintenancePlans",
                columns: new[] { "PlannedDate", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Measurements_EquipmentId_MeasuredAtUtc",
                table: "Measurements",
                columns: new[] { "EquipmentId", "MeasuredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Measurements_EquipmentId_Type_MeasuredAtUtc",
                table: "Measurements",
                columns: new[] { "EquipmentId", "Type", "MeasuredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "UX_Thresholds_EquipmentId_MeasurementType",
                table: "Thresholds",
                columns: new[] { "EquipmentId", "MeasurementType" },
                unique: true,
                filter: "\"EquipmentId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_Thresholds_Global_MeasurementType",
                table: "Thresholds",
                column: "MeasurementType",
                unique: true,
                filter: "\"EquipmentId\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Warnings_EquipmentId_Status_CreatedAtUtc",
                table: "Warnings",
                columns: new[] { "EquipmentId", "Status", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Warnings_MeasurementId_ThresholdId",
                table: "Warnings",
                columns: new[] { "MeasurementId", "ThresholdId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Warnings_Status_CreatedAtUtc",
                table: "Warnings",
                columns: new[] { "Status", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MaintenancePlans");

            migrationBuilder.DropTable(
                name: "Thresholds");

            migrationBuilder.DropTable(
                name: "Warnings");

            migrationBuilder.DropTable(
                name: "Measurements");

            migrationBuilder.DropTable(
                name: "Equipment");
        }
    }
}
