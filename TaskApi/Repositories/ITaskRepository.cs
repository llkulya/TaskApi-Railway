using TaskApi.Dto.Queries;
using TaskApi.Models;

namespace TaskApi.Repositories
{
    public interface ITaskRepository
    {
        Task<List<TaskItem>> GetAllAsync();
        Task<TaskItem?> GetByIdAsync(int id);
        Task<TaskItem> AddAsync(TaskItem item);
        Task<TaskItem?> UpdateAsync(TaskItem item);
        Task<bool> DeleteAsync(int id);
        Task<List<TaskItem>> GetHighPriorityAsync();
        Task<List<TaskItem>> GetFilteredAsync(TaskItemFilterQuery query);
        Task<int> GetTotalCountAsync(TaskItemFilterQuery query);
    }
}