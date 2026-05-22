using TaskApi.Dto.Commands; 
using TaskApi.Dto.Responses;

namespace TaskApi.Services
{
    public interface IProjectManagerService
    {
        Task<List<ProjectManagerDto>> GetAllAsync();
        Task<ProjectManagerDto?> GetByIdAsync(int id);
        Task<ProjectManagerDto> CreateAsync(ProjectManagerCreateCommand command); // Змінено тут
    }
}