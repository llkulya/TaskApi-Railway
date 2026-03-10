using Microsoft.AspNetCore.Mvc;
using TaskApi.Dto.Commands;
using TaskApi.Dto.Responses;
using TaskApi.Services;
using TaskApi.Exceptions;
using System.ComponentModel.DataAnnotations;

namespace TaskApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TasksController : ControllerBase
    {
        private readonly ITaskService _taskService;

        public TasksController(ITaskService taskService)
        {
            _taskService = taskService;
        }

        /// <summary>
        /// Отримати всі завдання
        /// </summary>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<List<TaskItemDto>>> GetAll()
        {
            var tasks = await _taskService.GetAllAsync();
            return Ok(tasks);
        }

        /// <summary>
        /// Отримати завдання за ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<TaskItemDto>> GetById(int id)
        {
            var task = await _taskService.GetByIdAsync(id);
            if (task == null)
                return NotFound($"Завдання з ID {id} не знайдено");
            return Ok(task);
        }

        /// <summary>
        /// Створити нове завдання
        /// </summary>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<TaskItemDto>> Create([FromBody] TaskItemCreateCommand command)
        {
            try
            {
                var createdTask = await _taskService.CreateAsync(command);
                return CreatedAtAction(nameof(GetById), new { id = createdTask.Id }, createdTask);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Оновити завдання
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<TaskItemDto>> Update(int id, [FromBody] TaskItemUpdateCommand command)
        {
            try
            {
                if (id != command.Id) return BadRequest("ID mismatch");

                var updatedTask = await _taskService.UpdateAsync(command);
                return Ok(updatedTask);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (ConcurrencyException ex)
            {
                // 409 Conflict - конфлікт оптимістичного блокування
                return Conflict(new { message = ex.Message, type = "ConcurrencyError" });
            }
            catch (ValidationException ex)
            {
                // 400 Bad Request - помилка валідації
                return BadRequest(new { message = ex.Message, type = "ValidationError" });
            }
        }

        /// <summary>
        /// Видалити завдання
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<DeleteTaskItemResponse>> Delete(int id)
        {
            var result = await _taskService.DeleteAsync(id);
            if (!result.Success)
                return NotFound(result);
            return Ok(result);
        }

        /// <summary>
        /// Отримати завдання з високим пріоритетом (High або Critical)
        /// </summary>
        [HttpGet("high-priority")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<List<TaskItemDto>>> GetHighPriority()
        {
            var tasks = await _taskService.GetHighPriorityAsync();
            return Ok(tasks);
        }
    }
}