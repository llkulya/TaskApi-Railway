using TaskApi.Dto.Queries;
using TaskApi.Models;

namespace TaskApi.Repositories
{
    public interface ITaskRepository : IBaseRepository<TaskItem>
    {
        Task<List<TaskItem>> GetHighPriorityAsync();
        Task<List<TaskItem>> GetFilteredAsync(TaskItemFilterQuery query);
        Task<int> GetTotalCountAsync(TaskItemFilterQuery query);
    }
}