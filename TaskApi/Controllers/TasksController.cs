using Microsoft.AspNetCore.Mvc;
using TaskApi.Dto.Commands;
using TaskApi.Dto.Queries;
using TaskApi.Dto.Responses;
using TaskApi.Exceptions;
using TaskApi.Services;
using System.ComponentModel.DataAnnotations;

namespace TaskApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TasksController : ControllerBase
    {
        private readonly ITaskService _taskService;
        private readonly IAssignmentService _assignmentService;

        public TasksController(ITaskService taskService, IAssignmentService assignmentService)
        {
            _taskService = taskService;
            _assignmentService = assignmentService;
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
        /// Отримати завдання з фільтрацією та пагінацією
        /// </summary>
        [HttpGet("filtered")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<PagedResult<TaskItemDto>>> GetFiltered(
            [FromQuery] TaskItemFilterQuery query)
        {
            var result = await _taskService.GetFilteredAsync(query);
            return Ok(result);
        }

        /// <summary>
        /// Отримати завдання з високим пріоритетом
        /// </summary>
        [HttpGet("high-priority")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<List<TaskItemDto>>> GetHighPriority()
        {
            var tasks = await _taskService.GetHighPriorityAsync();
            return Ok(tasks);
        }

        /// <summary>
        /// Створити нове завдання
        /// </summary>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<TaskItemDto>> Create(
            [FromBody] TaskItemCreateCommand command)
        {
            try
            {
                var createdTask = await _taskService.CreateAsync(command);
                return CreatedAtAction(nameof(GetById),
                    new { id = createdTask.Id }, createdTask);
            }
            catch (ValidationException ex)
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
        public async Task<ActionResult<TaskItemDto>> Update(
            int id, [FromBody] TaskItemUpdateCommand command)
        {
            command.Id = id;
            try
            {
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
            catch (ValidationException ex)
            {
                return BadRequest(new { message = ex.Message, type = "ValidationError" });
            }
            catch (ConcurrencyException ex)
            {
                return Conflict(new { message = ex.Message, type = "ConcurrencyError" });
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
        /// Юз-кейс 4.1 — Призначити виконавця на завдання
        /// </summary>
        [HttpPut("{taskId}/assign-executor")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> AssignExecutor(
            int taskId, [FromBody] AssignExecutorCommand command)
        {
            command.TaskId = taskId;
            try
            {
                await _assignmentService.AssignTaskToExecutorAsync(command);
                return Ok(new { Message = "Виконавця успішно призначено" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (ConcurrencyException ex)
            {
                return Conflict(new { message = ex.Message, type = "ConcurrencyError" });
            }
        }

        /// <summary>
        /// Юз-кейс 4.2 — Змінити виконавця завдання
        /// </summary>
        [HttpPut("{taskId}/change-executor")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> ChangeExecutor(
            int taskId, [FromBody] ChangeExecutorCommand command)
        {
            command.TaskId = taskId;
            try
            {
                await _assignmentService.ChangeExecutorAsync(command);
                return Ok(new { Message = "Виконавця успішно змінено" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (ConcurrencyException ex)
            {
                return Conflict(new { message = ex.Message, type = "ConcurrencyError" });
            }
        }
    }
}