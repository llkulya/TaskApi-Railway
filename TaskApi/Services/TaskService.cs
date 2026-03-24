using System.ComponentModel.DataAnnotations;
using TaskApi.Dto.Commands;
using TaskApi.Dto.Queries;
using TaskApi.Dto.Responses;
using TaskApi.Exceptions; 
using TaskApi.Models;
using TaskApi.Repositories;

namespace TaskApi.Services
{
    public class TaskService : ITaskService
    {
        private readonly ITaskRepository _repository;

        public TaskService(ITaskRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<List<TaskItemDto>> GetAllAsync()
        {
            var tasks = await _repository.GetAllAsync();
            return tasks.Select(MapToDto).ToList();
        }

        public async Task<TaskItemDto?> GetByIdAsync(int id)
        {
            var task = await _repository.GetByIdAsync(id);
            return task == null ? null : MapToDto(task);
        }

        public async Task<TaskItemDto> CreateAsync(TaskItemCreateCommand command)
        {

            if (string.IsNullOrWhiteSpace(command.Title))
                throw new ValidationException("Title is required");

            if (!Enum.TryParse<Models.TaskStatus>(command.Status, true, out var status))
            {
                status = Models.TaskStatus.Pending; // Тепер назва збігається з твоїм enum
            }

            if (!Enum.TryParse<Models.TaskPriority>(command.Priority, true, out var priority))
            {
                priority = Models.TaskPriority.Low;
            }

            var task = new TaskItem
            {
                Title = command.Title,
                Description = command.Description,
                Status = status,
                Priority = priority,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow,
                Version = 0
            };

            var createdTask = await _repository.AddAsync(task);
            return MapToDto(createdTask);
        }

        public async Task<TaskItemDto?> UpdateAsync(TaskItemUpdateCommand command)
        {
            var existingTask = await _repository.GetByIdAsync(command.Id);
            if (existingTask == null)
            {
                throw new KeyNotFoundException($"Task with ID {command.Id} is not found.");
            }

            // Оптимістичне блокування (перевірка версії)
            if (existingTask.Version != command.Version)
            {
                throw new ConcurrencyException(
                    $"Task has been modified. Current version: {existingTask.Version}, your version: {command.Version}");
            }

            var status = existingTask.Status;
            var priority = existingTask.Priority;

            if (!string.IsNullOrWhiteSpace(command.Status))
            {
                if (Enum.TryParse<Models.TaskStatus>(command.Status, true, out var parsedStatus))
                    status = parsedStatus;
            }

            if (!string.IsNullOrWhiteSpace(command.Priority))
            {
                if (Enum.TryParse<Models.TaskPriority>(command.Priority, true, out var parsedPriority))
                    priority = parsedPriority;
            }

            // Бізнес-логіка: заборона повернення з Done в InProgress
            if (existingTask.Status == Models.TaskStatus.Done && status == Models.TaskStatus.InProgress)
            {
                throw new InvalidOperationException("Cannot change status from Done to InProgress");
            }

            // Оновлення полів
            if (!string.IsNullOrWhiteSpace(command.Title)) existingTask.Title = command.Title;
            if (!string.IsNullOrWhiteSpace(command.Description)) existingTask.Description = command.Description;

            existingTask.Status = status;
            existingTask.Priority = priority;
            existingTask.ModifiedDate = DateTime.UtcNow;
            existingTask.Version++;

            var updatedTask = await _repository.UpdateAsync(existingTask);
            return updatedTask == null ? null : MapToDto(updatedTask);
        }

        public async Task<DeleteTaskItemResponse> DeleteAsync(int id)
        {
            var taskToDelete = await _repository.GetByIdAsync(id);
            if (taskToDelete == null)
            {
                return new DeleteTaskItemResponse
                {
                    Id = id,
                    Success = false,
                    Message = $"Task with ID {id} not found"
                };
            }

            var deleted = await _repository.DeleteAsync(id);

            return new DeleteTaskItemResponse
            {
                Id = taskToDelete.Id,
                Title = taskToDelete.Title,
                Success = deleted,
                Message = deleted ? "Task deleted successfully" : "Failed to delete task"
            };
        }

        public async Task<List<TaskItemDto>> GetHighPriorityAsync()
        {
            var tasks = await _repository.GetHighPriorityAsync();
            return tasks.Select(MapToDto).ToList();
        }

        private TaskItemDto MapToDto(TaskItem task)
        {
            return new TaskItemDto
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                Status = task.Status.ToString(),
                Priority = task.Priority.ToString(),

                ExecutorId = task.ExecutorId,
                CreatedDate = task.CreatedDate,

                Version = task.Version
            };
        }

        public async Task<PagedResult<TaskItemDto>> GetFilteredAsync(TaskItemFilterQuery query)
        {
            var totalCount = await _repository.GetTotalCountAsync(query);
            var items = await _repository.GetFilteredAsync(query);

            return new PagedResult<TaskItemDto>
            {
                Items = items.Select(MapToDto).ToList(),
                TotalCount = totalCount,
                PageNumber = query.Page,
                PageSize = query.PageSize
            };
        }
    }
}