using Microsoft.AspNetCore.Mvc;
using TaskApi.Dto;
using TaskApi.Dto.Commands;
using TaskApi.Services;

namespace TaskApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProjectsController : ControllerBase
    {
        private readonly IProjectService _projectService;

        public ProjectsController(IProjectService projectService)
        {
            _projectService = projectService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var projects = await _projectService.GetAllAsync();
            return Ok(projects);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ProjectCreateCommand command)
        {
            var project = await _projectService.CreateAsync(command);
            return Ok(project);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id, [FromQuery] string? reason = null)
        {
            var deleted = await _projectService.DeleteAsync(id, reason);

            if (!deleted)
                return NotFound(new { message = $"Project with ID {id} was not found." });

            return NoContent();
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> ChangeStatus(
    int id,
    [FromBody] ProjectStatusUpdateCommand command)
        {
            var updatedProject = await _projectService.ChangeStatusAsync(id, command);

            if (updatedProject == null)
                return NotFound(new { message = $"Project with ID {id} was not found." });

            return Ok(updatedProject);
        }
    }
}