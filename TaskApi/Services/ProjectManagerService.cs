using TaskApi.Dto.Commands;
using TaskApi.Dto.Responses; // Створимо це DTO нижче
using TaskApi.Models;
using TaskApi.Repositories;

namespace TaskApi.Services
{
    public class ProjectManagerService : IProjectManagerService
    {
        private readonly IProjectManagerRepository _repository;

        public ProjectManagerService(IProjectManagerRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<ProjectManagerDto>> GetAllAsync()
        {
            var managers = await _repository.GetAllAsync();
            return managers.Select(m => new ProjectManagerDto
            {
                Id = m.Id,
                FirstName = m.FirstName,
                LastName = m.LastName,
                Email = m.Email
            }).ToList();
        }

        public async Task<ProjectManagerDto?> GetByIdAsync(int id)
        {
            var m = await _repository.GetByIdAsync(id);
            return m == null ? null : new ProjectManagerDto
            {
                Id = m.Id,
                FirstName = m.FirstName,
                LastName = m.LastName,
                Email = m.Email
            };
        }

        public async Task<ProjectManagerDto> CreateAsync(ProjectManagerCreateCommand command)
        {
            var manager = new ProjectManager
            {
                FirstName = command.FirstName,
                LastName = command.LastName,
                Email = command.Email
            };

            var created = await _repository.AddAsync(manager);

            return new ProjectManagerDto
            {
                Id = created.Id,
                FirstName = created.FirstName,
                LastName = created.LastName,
                Email = created.Email
            };
        }
    }
}