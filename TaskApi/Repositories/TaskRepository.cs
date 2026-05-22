using Microsoft.EntityFrameworkCore;
using TaskApi.Data;
using TaskApi.Dto.Queries;
using TaskApi.Dto.Responses;
using TaskApi.Models;
using TaskPriority = TaskApi.Models.TaskPriority;
using TaskStatus = TaskApi.Models.TaskStatus;

namespace TaskApi.Repositories
{
    public class TaskRepository : BaseRepository<TaskItem>, ITaskRepository
    {
        public TaskRepository(ApplicationDbContext context) : base(context)
        {
        }

        // ПЕРЕВИЗНАЧАЄМО GetAllAsync, щоб підтягнути всі зв'язки
        public override async Task<List<TaskItem>> GetAllAsync()
        {
            return await _dbSet
                .Include(t => t.Project)
                .Include(t => t.Executor)
                .Include(t => t.Comments) // Додаємо коментарі!
                .ToListAsync();
        }

        // ПЕРЕВИЗНАЧАЄМО GetByIdAsync для детального перегляду
        public override async Task<TaskItem?> GetByIdAsync(int id)
        {
            return await _dbSet
                .Include(t => t.Project)
                .Include(t => t.Executor)
                .Include(t => t.Comments) // Додаємо коментарі
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<List<TaskItem>> GetHighPriorityAsync()
        {
            return await _dbSet
                .Include(t => t.Project)
                .Include(t => t.Executor)
                .Include(t => t.Comments) 
                .Where(t => t.Priority == TaskPriority.High ||
                            t.Priority == TaskPriority.Critical)
                .ToListAsync();
        }

        public async Task<PagedResult<TaskItem>> GetFilteredAsync(TaskItemFilterQuery query)
        {
            var filtered = _dbSet
                .Include(t => t.Project)
                .Include(t => t.Executor)
                .Include(t => t.Comments)
                .AsQueryable();

            // 🔍 Фільтрація
            if (query.ExecutorId.HasValue)
                filtered = filtered.Where(t => t.ExecutorId == query.ExecutorId.Value);

            if (!string.IsNullOrEmpty(query.Status) &&
                Enum.TryParse<TaskStatus>(query.Status, true, out var status))
                filtered = filtered.Where(t => t.Status == status);

            if (!string.IsNullOrEmpty(query.Priority) &&
                Enum.TryParse<TaskPriority>(query.Priority, true, out var priority))
                filtered = filtered.Where(t => t.Priority == priority);

            if (!string.IsNullOrEmpty(query.Search))
            {
                var search = query.Search.ToLower();

                filtered = filtered.Where(t =>
                    t.Title.ToLower().Contains(search) ||
                    (t.Description ?? "").ToLower().Contains(search));
            }

            if (query.CreatedAfter.HasValue)
                filtered = filtered.Where(t => t.CreatedDate >= query.CreatedAfter.Value);

            if (query.CreatedBefore.HasValue)
                filtered = filtered.Where(t => t.CreatedDate <= query.CreatedBefore.Value);

            if (query.IsOverdue == true)
                filtered = filtered.Where(t =>
                    t.DueDate < DateTime.UtcNow &&
                    t.Status != TaskStatus.Done);

            var totalCount = await filtered.CountAsync();

            filtered = query.SortBy?.ToLower() switch
            {
                "priority" => query.SortDescending
                    ? filtered.OrderByDescending(t => t.Priority)
                    : filtered.OrderBy(t => t.Priority),

                "duedate" => query.SortDescending
                    ? filtered.OrderByDescending(t => t.DueDate)
                    : filtered.OrderBy(t => t.DueDate),

                _ => query.SortDescending
                    ? filtered.OrderByDescending(t => t.CreatedDate)
                    : filtered.OrderBy(t => t.CreatedDate)
            };

            var items = await filtered
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            return new PagedResult<TaskItem>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = query.Page,
                PageSize = query.PageSize
            };
        }

        public async Task<int> GetTotalCountAsync(TaskItemFilterQuery query)
        {
            var filtered = _dbSet.AsQueryable();

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
        public async Task<TaskItem?> GetByIdWithCommentsAsync(int id)
        {
            return await _context.Tasks
                .Include(t => t.Comments)
                .FirstOrDefaultAsync(t => t.Id == id);
        }
        public async Task<List<TaskItem>> GetByUserIdAsync(int userId)
        {
            return await _context.Tasks
                .Where(t => t.ExecutorId == userId)
                .ToListAsync();
        }
    }
}