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
        [InlineData(MaintenancePlanStatus.Planned)]
        [InlineData(MaintenancePlanStatus.Completed)]
        [InlineData(MaintenancePlanStatus.Cancelled)]
        public void TryChangeStatus_WhenAlreadyTerminal_ShouldNotChangeStatus(
            MaintenancePlanStatus secondStatus)
        {
            var plan = CreatePlan();
            Assert.True(plan.TryChangeStatus(MaintenancePlanStatus.Completed));

            var changed = plan.TryChangeStatus(secondStatus);

            Assert.False(changed);
            Assert.Equal(MaintenancePlanStatus.Completed, plan.Status);
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
