using TaskApi.Dto.Commands;
using TaskApi.Dto.Responses;

namespace TaskApi.Services
{
    public interface IProjectService
    {
        Task<List<ProjectDto>> GetAllAsync();
        Task<ProjectDto?> GetByIdAsync(int id);
        Task<ProjectDto> CreateAsync(ProjectCreateCommand command);
        Task<bool> DeleteAsync(int id, string? reason = null);
        Task<ProjectDto?> ChangeStatusAsync(int id, ProjectStatusUpdateCommand command);
    }
}