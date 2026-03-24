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
        public async Task<IActionResult> Create(ProjectCreateCommand command)
        {
            var project = await _projectService.CreateAsync(command);
            return Ok(project);
        }
    }
}