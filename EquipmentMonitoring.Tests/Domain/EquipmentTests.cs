using EquipmentMonitoring.Domain.Entities;
using EquipmentMonitoring.Domain.Enums;

namespace EquipmentMonitoring.Tests.Domain
{
    public class EquipmentTests
    {
        [Fact]
        public void Constructor_WithValidData_ShouldCreateEquipmentWithGeneratedId()
        {
            var name = "Compressor A1";
            var type = "Compressor";
            var serialNumber = "SN-2026-0001";
            var location = "Workshop 1";
            var status = EquipmentStatus.Working;

            var equipment = new Equipment(name, type, serialNumber, location, status);

            Assert.NotEqual(Guid.Empty, equipment.Id);
            Assert.Equal(name, equipment.Name);
            Assert.Equal(type, equipment.Type);
            Assert.Equal(serialNumber, equipment.SerialNumber);
            Assert.Equal(location, equipment.Location);
            Assert.Equal(status, equipment.Status);
        }
    }
}
