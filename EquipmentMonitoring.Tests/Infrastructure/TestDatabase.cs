using EquipmentMonitoring.Domain.Entities;
using EquipmentMonitoring.Domain.Enums;
using EquipmentMonitoring.Infrastructure.Persistence;

namespace EquipmentMonitoring.Tests.Infrastructure
{
    internal abstract class TestDatabase : IAsyncDisposable
    {
        public abstract EquipmentMonitoringDbContext CreateContext();

        public async Task<Equipment> AddEquipmentAsync(string serialNumber, string name = "Compressor")
        {
            var equipment = new Equipment(name, "Compressor", serialNumber, "Workshop 1", EquipmentStatus.Working);

            await using var context = CreateContext();
            context.Equipment.Add(equipment);
            await context.SaveChangesAsync();

            return equipment;
        }

        public async Task<Measurement> AddMeasurementAsync(Guid equipmentId, MeasurementType type, double value, DateTime measuredAtUtc)
        {
            var measurement = new Measurement(equipmentId, type, value, type.GetUnit(), measuredAtUtc);

            await using var context = CreateContext();
            context.Measurements.Add(measurement);
            await context.SaveChangesAsync();

            return measurement;
        }

        public async Task<Warning> AddWarningAsync(
            Guid equipmentId,
            MeasurementType type,
            DateTime createdAtUtc,
            bool acknowledged = false)
        {
            var measurement = await AddMeasurementAsync(equipmentId, type, 60, createdAtUtc);
            var threshold = Threshold.CreateGlobal(type, null, 50, createdAtUtc);
            var warning = Warning.FromBreach(measurement, threshold, threshold.Evaluate(measurement.Value)!, createdAtUtc);

            if (acknowledged)
            {
                warning.Acknowledge(createdAtUtc.AddMinutes(1));
            }

            await using var context = CreateContext();
            context.Warnings.Add(warning);
            await context.SaveChangesAsync();

            return warning;
        }

        public abstract ValueTask DisposeAsync();
    }
}
