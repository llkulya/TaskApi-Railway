using TaskApi.Dto.Commands;
using TaskApi.Dto.Responses;

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
    }
}
