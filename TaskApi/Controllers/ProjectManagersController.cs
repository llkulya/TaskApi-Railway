using Microsoft.AspNetCore.Mvc;
using TaskApi.Models;
using TaskApi.Repositories;

namespace TaskApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProjectManagersController : ControllerBase
    {
        private readonly IProjectManagerRepository _repository;

        public ProjectManagersController(IProjectManagerRepository repository)
        {
            _repository = repository;
        }

        /// <summary>
        /// Отримати всіх менеджерів проектів
        /// </summary>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll()
        {
            var managers = await _repository.GetAllAsync();
            return Ok(managers);
        }

        /// <summary>
        /// Отримати менеджера за ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var manager = await _repository.GetByIdAsync(id);
            if (manager == null)
                return NotFound($"Менеджер з ID {id} не знайдено");
            return Ok(manager);
        }

        /// <summary>
        /// Створити нового менеджера проекту
        /// </summary>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public async Task<IActionResult> Create([FromBody] ProjectManager manager)
        {
            var created = await _repository.AddAsync(manager);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
    }
}