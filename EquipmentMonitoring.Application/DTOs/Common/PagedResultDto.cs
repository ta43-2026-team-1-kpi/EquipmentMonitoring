using System.ComponentModel;

namespace EquipmentMonitoring.Application.DTOs.Common
{
    [Description("Сторінка результатів.")]
    public class PagedResultDto<T>
    {
        [Description("Елементи поточної сторінки.")]
        public IReadOnlyList<T> Items { get; init; } = [];

        [Description("Номер сторінки, починаючи з 1.")]
        public int Page { get; init; }

        [Description("Розмір сторінки.")]
        public int PageSize { get; init; }

        [Description("Загальна кількість елементів за фільтром.")]
        public int TotalCount { get; init; }

        [Description("Загальна кількість сторінок.")]
        public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    }
}
