    namespace TaskApi.Services
{
    using TaskApi.Dto;
    using TaskApi.Dto.Commands;
    using TaskApi.Models;

    public interface IProjectService
    {
        Task<List<Project>> GetAllAsync();
        Task<Project> CreateAsync(ProjectCreateCommand command);
    }
}
