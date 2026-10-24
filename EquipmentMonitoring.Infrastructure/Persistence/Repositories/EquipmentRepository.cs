using EquipmentMonitoring.Application.Abstractions.Persistence;
using EquipmentMonitoring.Application.Exceptions;
using EquipmentMonitoring.Domain.Entities;
using EquipmentMonitoring.Domain.Enums;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EquipmentMonitoring.Infrastructure.Persistence.Repositories
{
    public class EquipmentRepository : IEquipmentRepository
    {
        private const int SqliteConstraintUniqueExtendedErrorCode = 2067;

        private readonly EquipmentMonitoringDbContext _dbContext;

        public EquipmentRepository(EquipmentMonitoringDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddAsync(
            Equipment equipment,
            CancellationToken cancellationToken = default)
        {
            await _dbContext.Equipment.AddAsync(
                equipment, cancellationToken);
        }

        public async Task<IReadOnlyList<Equipment>> GetAllAsync(
            EquipmentStatus? status = null,
            CancellationToken cancellationToken = default)
        {
            IQueryable<Equipment> query =
                _dbContext.Equipment.AsNoTracking();

            if (status is not null)
            {
                query = query.Where(e => e.Status == status.Value);
            }

            return await query.ToListAsync(cancellationToken);
        }

        public Task<bool> ExistsBySerialNumberAsync(
            string serialNumber,
            CancellationToken cancellationToken = default)
        {
            return _dbContext.Equipment.AnyAsync(
                e => e.SerialNumber == serialNumber,
                cancellationToken);
        }

        public async Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            try
            {
                return await _dbContext.SaveChangesAsync(
                    cancellationToken);
            }
            catch (DbUpdateException exception)
                when (IsDuplicateSerialNumberViolation(exception))
            {
                var serialNumber = exception.Entries
                    .Select(entry => entry.Entity)
                    .OfType<Equipment>()
                    .Select(equipment => equipment.SerialNumber)
                    .FirstOrDefault() ?? string.Empty;

                throw new DuplicateEquipmentSerialNumberException(
                    serialNumber, exception);
            }
        }

        private static bool IsDuplicateSerialNumberViolation(
            DbUpdateException exception)
        {
            if (exception.InnerException is not SqliteException sqliteException)
            {
                return false;
            }

            if (sqliteException.SqliteExtendedErrorCode
                != SqliteConstraintUniqueExtendedErrorCode)
            {
                return false;
            }

            return sqliteException.Message.Contains(
                    "Equipment.SerialNumber",
                    StringComparison.Ordinal)
                || sqliteException.Message.Contains(
                    "IX_Equipment_SerialNumber",
                    StringComparison.Ordinal);
        }
    }
}
