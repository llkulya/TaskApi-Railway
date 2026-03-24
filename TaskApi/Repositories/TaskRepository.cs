using TaskApi.Dto.Queries;
using TaskApi.Models;
using TaskStatus = TaskApi.Models.TaskStatus;
using TaskPriority = TaskApi.Models.TaskPriority;

namespace TaskApi.Repositories
{
    public class TaskRepository : BaseRepository<TaskItem>, ITaskRepository
    {
        public TaskRepository()
        {
            // Ініціалізація тестовими даними
            InitializeTestData();
        }

        private void InitializeTestData()
        {
            _entities.AddRange(new List<TaskItem>
            {
                new TaskItem
                {
                    Id = 1,
                    Title = "Завдання 1",
                    Description = "Опис завдання 1",
                    Status = TaskStatus.Pending,
                    Priority = TaskPriority.Medium,
                    CreatedDate = DateTime.UtcNow.AddDays(-2),
                    Version = 0
                },
                new TaskItem
                {
                    Id = 2,
                    Title = "Завдання 2",
                    Description = "Опис завдання 2",
                    Status = TaskStatus.InProgress,
                    Priority = TaskPriority.High,
                    CreatedDate = DateTime.UtcNow.AddDays(-1),
                    Version = 0
                },
                new TaskItem
                {
                    Id = 3,
                    Title = "Завдання 3",
                    Description = "Опис завдання 3",
                    Status = TaskStatus.Done,
                    Priority = TaskPriority.Low,
                    CreatedDate = DateTime.UtcNow,
                    Version = 0
                }
            });
        }

        public Task<List<TaskItem>> GetHighPriorityAsync()
        {
            var result = _entities
                .Where(t => t.Priority == TaskPriority.High ||
                            t.Priority == TaskPriority.Critical)
                .ToList();
            return Task.FromResult(result);
        }

        public Task<List<TaskItem>> GetFilteredAsync(TaskItemFilterQuery query)
        {
            var filtered = _entities.AsQueryable();

            // Фільтр за виконавцем
            if (query.ExecutorId.HasValue)
                filtered = filtered.Where(t => t.ExecutorId == query.ExecutorId.Value);

            // Фільтр за статусом
            if (!string.IsNullOrEmpty(query.Status) &&
                Enum.TryParse<TaskStatus>(query.Status, true, out var status))
                filtered = filtered.Where(t => t.Status == status);

            // Фільтр за пріоритетом
            if (!string.IsNullOrEmpty(query.Priority) &&
                Enum.TryParse<TaskPriority>(query.Priority, true, out var priority))
                filtered = filtered.Where(t => t.Priority == priority);

            // Пошук за назвою
            if (!string.IsNullOrEmpty(query.Search))
                filtered = filtered.Where(t =>
                    t.Title.Contains(query.Search, StringComparison.OrdinalIgnoreCase));

            // Пагінація
            var result = filtered
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToList();

            return Task.FromResult(result);
        }

        public Task<int> GetTotalCountAsync(TaskItemFilterQuery query)
        {
            var filtered = _entities.AsQueryable();

            if (query.ExecutorId.HasValue)
                filtered = filtered.Where(t => t.ExecutorId == query.ExecutorId.Value);

            if (!string.IsNullOrEmpty(query.Status) &&
                Enum.TryParse<TaskStatus>(query.Status, true, out var status))
                filtered = filtered.Where(t => t.Status == status);

            if (!string.IsNullOrEmpty(query.Priority) &&
                Enum.TryParse<TaskPriority>(query.Priority, true, out var priority))
                filtered = filtered.Where(t => t.Priority == priority);

            if (!string.IsNullOrEmpty(query.Search))
                filtered = filtered.Where(t =>
                    t.Title.Contains(query.Search, StringComparison.OrdinalIgnoreCase));

            return Task.FromResult(filtered.Count());
        }
    }
}