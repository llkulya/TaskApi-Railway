using TaskApi.Dto.Commands;
using TaskApi.Dto.Queries;
using TaskApi.Dto.Responses;
using TaskApi.Models;

namespace TaskApi.Services
{
    public interface ITaskService
    {
        Task<List<TaskItemDto>> GetAllAsync();
        Task<TaskItemDto?> GetByIdAsync(int id);
        Task<TaskItemDto> CreateAsync(TaskItemCreateCommand command);
        Task<TaskItemDto?> UpdateAsync(TaskItemUpdateCommand command);
        Task<DeleteTaskItemResponse> DeleteAsync(int id);
        Task<List<TaskItemDto>> GetHighPriorityAsync();
        Task<PagedResult<TaskItemDto>> GetFilteredAsync(TaskItemFilterQuery query);
        Task<TaskStatisticsDto> GetStatisticsAsync();
        Task<List<PriorityDistributionDto>> GetPriorityDistributionAsync();
        Task<BulkOperationResult> BulkUpdateStatusAsync(BulkUpdateStatusCommand command);
        Task<BulkOperationResult> BulkDeleteAsync(List<int> ids);
        Task<List<TaskHistory>> GetHistoryAsync(int taskId);
        Task<TaskItemDto?> GetByIdWithCommentsAsync(int id);
        Task<List<TaskItemDto>> GetTasksByUserAsync(int userId);
    }
}