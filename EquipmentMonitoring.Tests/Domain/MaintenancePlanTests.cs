using EquipmentMonitoring.Domain.Entities;
using EquipmentMonitoring.Domain.Enums;

namespace EquipmentMonitoring.Tests.Domain
{
    public class MaintenancePlanTests
    {
        [Fact]
        public void Constructor_ShouldSetPlannedStatusAndCopyPlanDetails()
        {
            var equipmentId = Guid.NewGuid();
            var plannedDate = new DateOnly(2026, 11, 12);

            var plan = new MaintenancePlan(
                equipmentId,
                plannedDate,
                "Annual inspection",
                "Olena Kovalenko",
                "Check the safety valves");

            Assert.NotEqual(Guid.Empty, plan.Id);
            Assert.Equal(equipmentId, plan.EquipmentId);
            Assert.Equal(plannedDate, plan.PlannedDate);
            Assert.Equal("Annual inspection", plan.WorkType);
            Assert.Equal("Olena Kovalenko", plan.ResponsibleEmployee);
            Assert.Equal("Check the safety valves", plan.Note);
            Assert.Equal(MaintenancePlanStatus.Planned, plan.Status);
        }

        [Theory]
        [InlineData(MaintenancePlanStatus.Completed)]
        [InlineData(MaintenancePlanStatus.Cancelled)]
        public void TryChangeStatus_FromPlanned_ShouldApplyTerminalStatus(
            MaintenancePlanStatus requestedStatus)
        {
            var plan = CreatePlan();

            var changed = plan.TryChangeStatus(requestedStatus);

            Assert.True(changed);
            Assert.Equal(requestedStatus, plan.Status);
        }

        [Theory]
        [InlineData(MaintenancePlanStatus.Completed, MaintenancePlanStatus.Planned)]
        [InlineData(MaintenancePlanStatus.Completed, MaintenancePlanStatus.Completed)]
        [InlineData(MaintenancePlanStatus.Completed, MaintenancePlanStatus.Cancelled)]
        [InlineData(MaintenancePlanStatus.Cancelled, MaintenancePlanStatus.Planned)]
        [InlineData(MaintenancePlanStatus.Cancelled, MaintenancePlanStatus.Completed)]
        [InlineData(MaintenancePlanStatus.Cancelled, MaintenancePlanStatus.Cancelled)]
        public void TryChangeStatus_WhenAlreadyTerminal_ShouldNotChangeStatus(
            MaintenancePlanStatus initialTerminalStatus,
            MaintenancePlanStatus secondStatus)
        {
            var plan = CreatePlan();
            Assert.True(plan.TryChangeStatus(initialTerminalStatus));

            var changed = plan.TryChangeStatus(secondStatus);

            Assert.False(changed);
            Assert.Equal(initialTerminalStatus, plan.Status);
        }

        private static MaintenancePlan CreatePlan()
        {
            return new MaintenancePlan(
                Guid.NewGuid(),
                new DateOnly(2026, 11, 12),
                "Annual inspection",
                "Olena Kovalenko",
                null);
        }
    }
}
