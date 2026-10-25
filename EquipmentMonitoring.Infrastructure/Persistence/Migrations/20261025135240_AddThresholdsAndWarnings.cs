using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EquipmentMonitoring.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddThresholdsAndWarnings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Thresholds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EquipmentId = table.Column<Guid>(type: "TEXT", nullable: true),
                    MeasurementType = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    MinValue = table.Column<double>(type: "REAL", nullable: true),
                    MaxValue = table.Column<double>(type: "REAL", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Thresholds", x => x.Id);
                    table.CheckConstraint("CK_Thresholds_AtLeastOneLimit", "MinValue IS NOT NULL OR MaxValue IS NOT NULL");
                    table.CheckConstraint("CK_Thresholds_MeasurementType_Allowed", "MeasurementType IN ('Temperature', 'Voltage', 'Load')");
                    table.CheckConstraint("CK_Thresholds_MinNotGreaterThanMax", "MinValue IS NULL OR MaxValue IS NULL OR MinValue <= MaxValue");
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
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EquipmentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MeasurementId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ThresholdId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ThresholdScope = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    MeasurementType = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    ActualValue = table.Column<double>(type: "REAL", nullable: false),
                    Unit = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    BreachType = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    LimitValue = table.Column<double>(type: "REAL", nullable: false),
                    MeasuredAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    AcknowledgedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Warnings", x => x.Id);
                    table.CheckConstraint("CK_Warnings_Acknowledgement_Consistent", "(Status = 'Active' AND AcknowledgedAtUtc IS NULL) OR (Status = 'Acknowledged' AND AcknowledgedAtUtc IS NOT NULL)");
                    table.CheckConstraint("CK_Warnings_BreachType_Allowed", "BreachType IN ('BelowMinimum', 'AboveMaximum')");
                    table.CheckConstraint("CK_Warnings_MeasurementType_Allowed", "MeasurementType IN ('Temperature', 'Voltage', 'Load')");
                    table.CheckConstraint("CK_Warnings_Status_Allowed", "Status IN ('Active', 'Acknowledged')");
                    table.CheckConstraint("CK_Warnings_ThresholdScope_Allowed", "ThresholdScope IN ('Global', 'Equipment')");
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
                name: "UX_Thresholds_EquipmentId_MeasurementType",
                table: "Thresholds",
                columns: new[] { "EquipmentId", "MeasurementType" },
                unique: true,
                filter: "EquipmentId IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_Thresholds_Global_MeasurementType",
                table: "Thresholds",
                column: "MeasurementType",
                unique: true,
                filter: "EquipmentId IS NULL");

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
                name: "Thresholds");

            migrationBuilder.DropTable(
                name: "Warnings");
        }
    }
}
