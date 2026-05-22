using TaskApi.Dto.Commands;
using TaskApi.Dto.Responses;
using System.Security.Claims;
using TaskApi.Models;
using TaskApi.Repositories;

namespace TaskApi.Services
{
    public class ProjectService : IProjectService
    {
        private readonly IProjectRepository _repository;
        private readonly IProjectManagerRepository _managerRepository;
        private readonly ILogger<ProjectService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ProjectService(
    IProjectRepository repository,
    IProjectManagerRepository managerRepository,
    ILogger<ProjectService> logger,
    IHttpContextAccessor httpContextAccessor)
        {
            _repository = repository;
            _managerRepository = managerRepository;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<List<ProjectDto>> GetAllAsync()
        {
            var projects = await _repository.GetAllAsync();
            return projects.Select(p => MapToDto(p)).ToList();
        }

        public async Task<ProjectDto?> GetByIdAsync(int id)
        {
            var project = await _repository.GetByIdAsync(id);
            return project == null ? null : MapToDto(project);
        }

        public async Task<ProjectDto> CreateAsync(ProjectCreateCommand command)
        {
            // Перевірка менеджера
            var manager = await _managerRepository.GetByIdAsync(command.ProjectManagerId);
            if (manager == null)
                throw new KeyNotFoundException($"Manager with ID {command.ProjectManagerId} not found.");

            var project = new Project
            {
                Name = command.Name,
                Description = command.Description,
                ManagerId = command.ProjectManagerId, // Підв'язка
                Status = ProjectStatus.Active,
                StartDate = DateTime.UtcNow
            };

            var created = await _repository.AddAsync(project);

            _logger.LogInformation(
                "Project {ProjectId} with title {ProjectTitle} created by user {UserId} at {Timestamp} from IP {IpAddress}",
                created.Id,
                created.Name,
                GetCurrentUserId(),
                DateTime.UtcNow,
                GetIpAddress());

            return MapToDto(created);
        }

        public async Task<bool> DeleteAsync(int id, string? reason = null)
        {
            var project = await _repository.GetByIdAsync(id);

            if (project == null)
            {
                _logger.LogWarning(
                    "Project deletion failed. Project {ProjectId} was not found. Requested by {DeletedBy}. Reason: {Reason}",
                    id,
                    GetCurrentUserId(),
                    reason ?? "not specified");

                return false;
            }

            var deletedBy = GetCurrentUserId();
            var projectTitle = project.Name;

            var deleted = await _repository.DeleteAsync(id);

            if (deleted)
            {
                _logger.LogWarning(
                    "Project {ProjectId} with title {ProjectTitle} deleted by {DeletedBy}. Reason: {Reason}",
                    id,
                    projectTitle,
                    deletedBy,
                    reason ?? "not specified");
            }

            return deleted;
        }

        public async Task<ProjectDto?> ChangeStatusAsync(
    int id,
    ProjectStatusUpdateCommand command)
        {
            var project = await _repository.GetByIdAsync(id);

            if (project == null)
            {
                _logger.LogWarning(
                    "Project status change failed. Project {ProjectId} was not found. Requested by {ChangedBy}. Reason: {Reason}",
                    id,
                    GetCurrentUserId(),
                    command.Reason ?? "not specified");

                return null;
            }

            if (!Enum.TryParse<ProjectStatus>(command.NewStatus, true, out var newStatus))
            {
                _logger.LogWarning(
                    "Project {ProjectId} status change failed. Invalid status {NewStatus}. ChangedBy: {ChangedBy}",
                    id,
                    command.NewStatus,
                    GetCurrentUserId());

                throw new InvalidOperationException(
                    $"Invalid project status '{command.NewStatus}'. Allowed values: Active, Completed, OnHold, Cancelled.");
            }

            var oldStatus = project.Status;

            if (oldStatus == newStatus)
            {
                _logger.LogInformation(
                    "Project {ProjectId} status was not changed because it is already {Status}. ChangedBy: {ChangedBy}",
                    id,
                    newStatus,
                    GetCurrentUserId());

                return MapToDto(project);
            }

            project.Status = newStatus;

            if (newStatus == ProjectStatus.Completed || newStatus == ProjectStatus.Cancelled)
            {
                project.EndDate = DateTime.UtcNow;
            }

            await _repository.UpdateAsync(project);

            _logger.LogInformation(
                "Project {ProjectId} status changed from {OldStatus} to {NewStatus} by {ChangedBy}. Reason: {Reason}",
                project.Id,
                oldStatus,
                newStatus,
                GetCurrentUserId(),
                command.Reason ?? "not specified");

            return MapToDto(project);
        }

        private string GetCurrentUserId()
        {
            return _httpContextAccessor.HttpContext?.User?
                .FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? _httpContextAccessor.HttpContext?.User?
                .FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                ?? "anonymous";
        }

        private string GetIpAddress()
        {
            return _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()
                ?? "unknown";
        }

        private ProjectDto MapToDto(Project project)
        {
            return new ProjectDto
            {
                Id = project.Id,
                Name = project.Name,
                Description = project.Description,
                Status = project.Status.ToString(),
                ProjectManagerId = project.ManagerId,

                // Склеюємо ім'я та прізвище, якщо об'єкт Manager завантажений
                ManagerFullName = project.Manager != null
                    ? $"{project.Manager.FirstName} {project.Manager.LastName}"
                    : "Unknown Manager"
            };
        }
    }
}