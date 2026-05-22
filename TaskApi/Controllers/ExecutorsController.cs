using Microsoft.AspNetCore.Mvc;
using TaskApi.Dto.Commands;
using TaskApi.Dto.Responses;
using TaskApi.Services;

namespace TaskApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ExecutorsController : ControllerBase
    {
        private readonly IExecutorService _executorService;

        public ExecutorsController(IExecutorService executorService)
        {
            _executorService = executorService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<ExecutorDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll()
        {
            var executors = await _executorService.GetAllAsync();
            return Ok(executors);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ExecutorDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var executor = await _executorService.GetByIdAsync(id);
            if (executor == null)
                return NotFound($"Executor with ID {id} was not found.");
            return Ok(executor);
        }

        [HttpPost]
        [ProducesResponseType(typeof(ExecutorDto), StatusCodes.Status201Created)]
        public async Task<IActionResult> Create(ExecutorCreateCommand command)
        {
            var executorDto = await _executorService.CreateAsync(command);
            return CreatedAtAction(nameof(GetById), new { id = executorDto.Id }, executorDto);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _executorService.DeleteAsync(id);
            if (!deleted) return NotFound();
            return NoContent(); 
        }
    }
}