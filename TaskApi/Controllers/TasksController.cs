using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskApi.Dto.Commands;
using TaskApi.Dto.Queries;
using TaskApi.Dto.Responses;
using TaskApi.Attributes;
using TaskApi.Services;

namespace TaskApi.Controllers
{
    [Authorize]
    [LogAccess]
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
        [ProducesResponseType(typeof(List<TaskItemDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll([FromQuery] TaskItemFilterQuery query)
        {
            // Викликаємо метод з фільтрацією
            var tasks = await _taskService.GetFilteredAsync(query);
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
            var createdTask = await _taskService.CreateAsync(command);
            return CreatedAtAction(nameof(GetById), new { id = createdTask.Id }, createdTask);
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
            var updatedTask = await _taskService.UpdateAsync(command);
            return Ok(updatedTask);
        }

        /// <summary>
        /// Видалити завдання
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<DeleteTaskItemResponse>> Delete(int id)
        {
            var result = await _taskService.DeleteAsync(id);
            if (!result.Success)
                return NotFound(result);
            return NoContent();
        }

        /// <summary>
        /// Юз-кейс: Масове оновлення статусів (Контрольний проєкт)
        /// </summary>
        [HttpPatch("bulk-status")]
        [ProducesResponseType(typeof(BulkOperationResult), StatusCodes.Status200OK)]
        public async Task<ActionResult<BulkOperationResult>> BulkUpdateStatus(
            [FromBody] BulkUpdateStatusCommand command)
        {
            var result = await _taskService.BulkUpdateStatusAsync(command);
            return Ok(result);
        }

        /// <summary>
        /// Отримати статистику та аналітику (Контрольний проєкт)
        /// </summary>
        [HttpGet("statistics")]
        [ProducesResponseType(typeof(TaskStatisticsDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<TaskStatisticsDto>> GetStatistics()
        {
            var stats = await _taskService.GetStatisticsAsync();
            return Ok(stats);
        }

        /// <summary>
        /// Юз-кейс: Масове видалення завдань (Контрольний проєкт)
        /// </summary>
        [HttpPost("bulk-delete")]
        [ProducesResponseType(typeof(BulkOperationResult), StatusCodes.Status200OK)]
        public async Task<ActionResult<BulkOperationResult>> BulkDelete(
            [FromBody] List<int> ids)
        {
            var result = await _taskService.BulkDeleteAsync(ids);
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
            await _assignmentService.AssignTaskToExecutorAsync(command);
            return Ok(new { Message = "Виконавця успішно призначено" });
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
            await _assignmentService.ChangeExecutorAsync(command);
            return Ok(new { Message = "Виконавця успішно змінено" });
        }

        [HttpGet("{id}/history")]
        public async Task<IActionResult> GetHistory(int id)
        {
            var history = await _taskService.GetHistoryAsync(id);
            return Ok(history);
        }

        /// <summary>
        /// Розподіл завдань за пріоритетами (Контрольний проєкт)
        /// </summary>
        [HttpGet("priority-distribution")]
        [ProducesResponseType(typeof(List<PriorityDistributionDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<PriorityDistributionDto>>> GetPriorityDistribution()
        {
            var result = await _taskService.GetPriorityDistributionAsync();
            return Ok(result);
        }
        /// <summary>
        /// Отримати завдання за ID користувача
        /// </summary>
        [HttpGet("{userId}/by-user")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<List<TaskItemDto>>> GetTasksByUser(int userId)
        {
            var tasks = await _taskService.GetTasksByUserAsync(userId);
            return Ok(tasks);
        }
    }
}