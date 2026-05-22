using Microsoft.AspNetCore.Mvc;
using TaskApi.Dto.Commands;
using TaskApi.Dto.Responses;
using TaskApi.Models;
using TaskApi.Services;

namespace TaskApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProjectManagersController : ControllerBase
    {
        private readonly IProjectManagerService _service; 

        public ProjectManagersController(IProjectManagerService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var managers = await _service.GetAllAsync();
            return Ok(managers);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var manager = await _service.GetByIdAsync(id);
            if (manager == null)
                return NotFound($"Менеджер з ID {id} не знайдено");
            return Ok(manager);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ProjectManagerCreateCommand command) // Змінено тут
        {
            var createdDto = await _service.CreateAsync(command);
            return CreatedAtAction(nameof(GetById), new { id = createdDto.Id }, createdDto);
        }
    }
}