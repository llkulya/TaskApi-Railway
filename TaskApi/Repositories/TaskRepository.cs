using Microsoft.EntityFrameworkCore;
using TaskApi.Data;
using TaskApi.Dto.Queries;
using TaskApi.Models;
using TaskStatus = TaskApi.Models.TaskStatus;
using TaskPriority = TaskApi.Models.TaskPriority;

namespace TaskApi.Repositories
{
    public class TaskRepository : BaseRepository<TaskItem>, ITaskRepository
    {
        // Конструктор передає контекст у базовий клас BaseRepository
        public TaskRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<List<TaskItem>> GetHighPriorityAsync()
        {
            // Використовуємо .Include(), щоб разом із завданням підтягнути Проєкт та Виконавця
            // Це захищає від помилок, коли в API приходить null замість назви проєкту
            return await _dbSet
                .Include(t => t.Project)
                .Include(t => t.Executor)
                .Where(t => t.Priority == TaskPriority.High ||
                            t.Priority == TaskPriority.Critical)
                .ToListAsync();
        }

        public async Task<List<TaskItem>> GetFilteredAsync(TaskItemFilterQuery query)
        {
            // Починаємо формувати запит до БД (IQueryable)
            var filtered = _dbSet
                .Include(t => t.Project)
                .Include(t => t.Executor)
                .AsQueryable();

            // Фільтрація за виконавцем
            if (query.ExecutorId.HasValue)
                filtered = filtered.Where(t => t.ExecutorId == query.ExecutorId.Value);

            // Фільтрація за статусом
            if (!string.IsNullOrEmpty(query.Status) &&
                Enum.TryParse<TaskStatus>(query.Status, true, out var status))
                filtered = filtered.Where(t => t.Status == status);

            // Фільтрація за пріоритетом
            if (!string.IsNullOrEmpty(query.Priority) &&
                Enum.TryParse<TaskPriority>(query.Priority, true, out var priority))
                filtered = filtered.Where(t => t.Priority == priority);

            // Пошук за назвою (тепер виконується на рівні SQL через LIKE)
            if (!string.IsNullOrEmpty(query.Search))
                filtered = filtered.Where(t => EF.Functions.Like(t.Title, $"%{query.Search}%"));

            // Пагінація та фінальне виконання запиту
            return await filtered
                .OrderByDescending(t => t.CreatedDate)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();
        }

        public async Task<int> GetTotalCountAsync(TaskItemFilterQuery query)
        {
            var filtered = _dbSet.AsQueryable();

            // Дублюємо логіку фільтрів для підрахунку загальної кількості (без пагінації)
            if (query.ExecutorId.HasValue)
                filtered = filtered.Where(t => t.ExecutorId == query.ExecutorId.Value);

            if (!string.IsNullOrEmpty(query.Status) &&
                Enum.TryParse<TaskStatus>(query.Status, true, out var status))
                filtered = filtered.Where(t => t.Status == status);

            if (!string.IsNullOrEmpty(query.Priority) &&
                Enum.TryParse<TaskPriority>(query.Priority, true, out var priority))
                filtered = filtered.Where(t => t.Priority == priority);

            if (!string.IsNullOrEmpty(query.Search))
                filtered = filtered.Where(t => EF.Functions.Like(t.Title, $"%{query.Search}%"));

            return await filtered.CountAsync();
        }
    }
}