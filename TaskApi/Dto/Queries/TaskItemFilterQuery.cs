using System.ComponentModel.DataAnnotations;

namespace TaskApi.Dto.Queries
{
    public class TaskItemFilterQuery
    {
        /// <summary>
        /// Фільтр за виконавцем
        /// </summary>
        public int? ExecutorId { get; set; }

        /// <summary>
        /// Фільтр за статусом
        /// </summary>
        public string? Status { get; set; }

        /// <summary>
        /// Фільтр за пріоритетом
        /// </summary>
        public string? Priority { get; set; }

        /// <summary>
        /// Пошук за назвою
        /// </summary>
        public string? Search { get; set; }

        /// <summary>
        /// Номер сторінки (для пагінації)
        /// </summary>
        [Range(1, int.MaxValue)]
        public int Page { get; set; } = 1;

        /// <summary>
        /// Розмір сторінки (для пагінації)
        /// </summary>
        [Range(1, 100)]
        public int PageSize { get; set; } = 10;

        /// <summary>
        /// Фільтр: створено після дати
        /// </summary>
        public DateTime? CreatedAfter { get; set; }

        /// <summary>
        /// Фільтр: створено до дати
        /// </summary>
        public DateTime? CreatedBefore { get; set; }

        /// <summary>
        /// Фільтр: прострочені задачі
        /// </summary>
        public bool? IsOverdue { get; set; }

        /// <summary>
        /// Сортування по полю (CreatedDate, Priority, DueDate)
        /// </summary>
        public string? SortBy { get; set; }

        /// <summary>
        /// Сортування за спаданням
        /// </summary>
        public bool SortDescending { get; set; } = true;
    }
}