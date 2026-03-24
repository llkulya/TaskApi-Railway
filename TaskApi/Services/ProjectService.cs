using TaskApi.Dto;
using TaskApi.Models;
using TaskApi.Repositories;

namespace TaskApi.Services
{
    public class ProjectService : IProjectService
    {
        private readonly IProjectRepository _projectRepository;

        public ProjectService(IProjectRepository projectRepository)
        {
            _projectRepository = projectRepository;
        }

        public async Task<List<Project>> GetAllAsync()
        {
            return await _projectRepository.GetAllAsync();
        }

        public async Task<Project> CreateAsync(ProjectCreateCommand command)
        {
            var project = new Project
            {
                Name = command.Name,
                Description = command.Description,
                StartDate = DateTime.UtcNow,
                Status = ProjectStatus.Active
            };
            return await _projectRepository.AddAsync(project);
        }
    }
}