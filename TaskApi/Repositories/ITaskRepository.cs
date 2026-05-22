using TaskApi.Dto.Queries;
using TaskApi.Dto.Responses;
using TaskApi.Models;

namespace TaskApi.Repositories
{
    public interface ITaskRepository : IBaseRepository<TaskItem>
    {
        Task<List<TaskItem>> GetHighPriorityAsync();
        Task<PagedResult<TaskItem>> GetFilteredAsync(TaskItemFilterQuery query);
        Task<int> GetTotalCountAsync(TaskItemFilterQuery query);
        Task<TaskItem?> GetByIdWithCommentsAsync(int id);
        Task<List<TaskItem>> GetByUserIdAsync(int userId);

    }
}