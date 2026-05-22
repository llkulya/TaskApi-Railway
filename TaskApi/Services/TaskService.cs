using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using TaskApi.Data;
using TaskApi.Dto.Commands;
using TaskApi.Dto.Queries;
using TaskApi.Dto.Responses;
using TaskApi.Exceptions;
using TaskApi.Models;
using TaskApi.Attributes;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using TaskApi.Repositories;

namespace TaskApi.Services
{
    [LogMethod(Level = LogLevel.Information)]
    [MeasureTime(WarningThresholdMs = 500, ErrorThresholdMs = 2000)]
    public class TaskService : ITaskService
    {
        private readonly ITaskRepository _repository;
        private readonly IProjectRepository _projectRepository;
        private readonly IExecutorRepository _executorRepository;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TaskService> _logger;                   
        private readonly IHttpContextAccessor _httpContextAccessor;

        // Оновлений конструктор
        public TaskService(
            ITaskRepository repository,
            IProjectRepository projectRepository,
            IExecutorRepository executorRepository,
            ApplicationDbContext context,
            ILogger<TaskService> logger,                                
        IHttpContextAccessor httpContextAccessor)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _projectRepository = projectRepository ?? throw new ArgumentNullException(nameof(projectRepository));
            _executorRepository = executorRepository ?? throw new ArgumentNullException(nameof(executorRepository));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));                           
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor)); 
        }
        private string GetIpAddress() =>
      _httpContextAccessor.HttpContext?
          .Connection.RemoteIpAddress?.ToString() ?? "unknown";

        private string GetCurrentUserId()
        {
            return _httpContextAccessor.HttpContext?.User?
                .FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? _httpContextAccessor.HttpContext?.User?
                .FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                ?? "SystemUser";
        }

        public async Task<List<TaskItemDto>> GetAllAsync()
        {
            try
            {
                _logger.LogInformation("Fetching all tasks");

                var tasks = await _repository.GetAllAsync();

                _logger.LogInformation(
                    "Retrieved {TaskCount} tasks",
                    tasks.Count);

                return tasks.Select(MapToDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error fetching all tasks");
                throw;
            }
        }

        [Cache(DurationSeconds = 60)]
        public async Task<TaskItemDto?> GetByIdAsync(int id)
        {
            var task = await _repository.GetByIdAsync(id);
            return task == null ? null : MapToDto(task);
        }

        public async Task<TaskItemDto?> GetByIdWithCommentsAsync(int id)
        {
            var task = await _repository.GetByIdWithCommentsAsync(id);
            return task == null ? null : MapToDto(task);
        }

        public async Task<TaskItemDto> CreateAsync(TaskItemCreateCommand command)
        {
            var ipAddress = GetIpAddress();

            try
            {
                _logger.LogInformation(
                    "Creating task {Title} from IP {IpAddress}",
                    command.Title, ipAddress);

                if (string.IsNullOrWhiteSpace(command.Title))
                    throw new ValidationException("Title is required");

                if (command.ProjectId.HasValue)
                {
                    var project = await _projectRepository.GetByIdAsync(command.ProjectId.Value);
                    if (project == null)
                    {
                        _logger.LogWarning(
                            "Task creation failed - project {ProjectId} not found",
                            command.ProjectId);
                        throw new KeyNotFoundException($"Project with ID {command.ProjectId} not found.");
                    }
                }

                if (command.ExecutorId.HasValue)
                {
                    var executor = await _executorRepository.GetByIdAsync(command.ExecutorId.Value);
                    if (executor == null)
                    {
                        _logger.LogWarning(
                            "Task creation failed - executor {ExecutorId} not found",
                            command.ExecutorId);
                        throw new KeyNotFoundException($"Executor with ID {command.ExecutorId} not found.");
                    }
                }

                if (!Enum.TryParse<Models.TaskStatus>(command.Status, true, out var status))
                    status = Models.TaskStatus.Pending;

                if (!Enum.TryParse<Models.TaskPriority>(command.Priority, true, out var priority))
                    priority = Models.TaskPriority.Low;

                var task = new TaskItem
                {
                    Title = command.Title,
                    Description = command.Description,
                    Status = status,
                    Priority = priority,
                    ProjectId = command.ProjectId,
                    ExecutorId = command.ExecutorId,
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow,
                    Version = 0
                };

                var createdTask = await _repository.AddAsync(task);

                // Critical пріоритет — окреме попередження
                if (priority == Models.TaskPriority.Critical)
                {
                    _logger.LogWarning(
                        "Task {TaskId} created with Critical priority. Title: {Title}, ProjectId: {ProjectId}",
                        createdTask.Id, createdTask.Title, createdTask.ProjectId);
                }
                else
                {
                    _logger.LogInformation(
                        "Task {TaskId} created successfully. Title: {Title}, Priority: {Priority}, ProjectId: {ProjectId}",
                        createdTask.Id, createdTask.Title, priority, createdTask.ProjectId);
                }

                return MapToDto(createdTask);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Task creation failed - dependency not found");
                throw;
            }
            catch (Exception ex) when (ex is not ValidationException)
            {
                _logger.LogError(ex,
                    "Unexpected error creating task {Title} from IP {IpAddress}",
                    command.Title, ipAddress);
                throw;
            }
        }

        public async Task<TaskItemDto?> UpdateAsync(TaskItemUpdateCommand command)
        {
            var existingTask = await _repository.GetByIdAsync(command.Id);
            if (existingTask == null)
            {
                throw new KeyNotFoundException($"Task with ID {command.Id} is not found.");
            }

            if (existingTask.Version != command.Version)
            {
                throw new ConcurrencyException(
                    $"Task has been modified. Current version: {existingTask.Version}, your version: {command.Version}");
            }

            if (existingTask.Status == Models.TaskStatus.Done && existingTask.CompletedAt.HasValue)
            {
                if (existingTask.CompletedAt.Value < DateTime.UtcNow.AddHours(-24))
                {
                    throw new InvalidOperationException("Cannot modify task after 24 hours of completion");
                }
            }

            var newStatus = existingTask.Status;
            var newPriority = existingTask.Priority;

            var oldPriority = existingTask.Priority;
            var changedBy = GetCurrentUserId();
            var reason = string.IsNullOrWhiteSpace(command.Reason)
                ? "not specified"
                : command.Reason;

            if (!string.IsNullOrWhiteSpace(command.Status))
            {
                if (Enum.TryParse<Models.TaskStatus>(command.Status, true, out var parsedStatus))
                    newStatus = parsedStatus;
            }

            if (!string.IsNullOrWhiteSpace(command.Priority))
            {
                if (Enum.TryParse<Models.TaskPriority>(command.Priority, true, out var parsedPriority))
                    newPriority = parsedPriority;
            }

            var priorityChangedToCritical =
    oldPriority != Models.TaskPriority.Critical &&
    newPriority == Models.TaskPriority.Critical;

            // Заборона повернення з Done
            if (existingTask.Status == Models.TaskStatus.Done && newStatus == Models.TaskStatus.InProgress)
            {
                throw new InvalidOperationException("Cannot change status from Done to InProgress");
            }

            // ІСТОРІЯ ЗМІН
            var historyEntries = new List<TaskHistory>();

            // Status
            if (existingTask.Status != newStatus)
            {
                historyEntries.Add(new TaskHistory
                {
                    TaskItemId = existingTask.Id,
                    ChangeDate = DateTime.UtcNow,
                    ChangeType = "Status",
                    OldValue = existingTask.Status.ToString(),
                    NewValue = newStatus.ToString(),
                    ChangedBy = changedBy
                });
            }

            // Title
            if (!string.IsNullOrWhiteSpace(command.Title) && existingTask.Title != command.Title)
            {
                historyEntries.Add(new TaskHistory
                {
                    TaskItemId = existingTask.Id,
                    ChangeDate = DateTime.UtcNow,
                    ChangeType = "Title",
                    OldValue = existingTask.Title,
                    NewValue = command.Title,
                    ChangedBy = changedBy
                });
            }

            // Priority
            if (existingTask.Priority != newPriority)
            {
                historyEntries.Add(new TaskHistory
                {
                    TaskItemId = existingTask.Id,
                    ChangeDate = DateTime.UtcNow,
                    ChangeType = "Priority",
                    OldValue = existingTask.Priority.ToString(),
                    NewValue = newPriority.ToString(),
                    ChangedBy = changedBy
                });
            }

            // ОНОВЛЕННЯ ДАНИХ

            if (!string.IsNullOrWhiteSpace(command.Title))
                existingTask.Title = command.Title;

            if (!string.IsNullOrWhiteSpace(command.Description))
                existingTask.Description = command.Description;

            // CompletedAt логіка
            if (existingTask.Status != newStatus)
            {
                if (newStatus == Models.TaskStatus.Done)
                {
                    existingTask.CompletedAt = DateTime.UtcNow;
                }

                if (existingTask.Status == Models.TaskStatus.Done && newStatus != Models.TaskStatus.Done)
                {
                    existingTask.CompletedAt = null;
                }

                existingTask.Status = newStatus;
            }

            existingTask.Priority = newPriority;
            existingTask.ModifiedDate = DateTime.UtcNow;
            existingTask.Version++;

            var updatedTask = await _repository.UpdateAsync(existingTask);

            if (updatedTask != null && priorityChangedToCritical)
            {
                _logger.LogWarning(
                    "Task {TaskId} priority changed from {OldPriority} to Critical by {ChangedBy}. Reason: {Reason}",
                    existingTask.Id,
                    oldPriority,
                    changedBy,
                    reason);
            }

            if (historyEntries.Any())
            {
                await _context.TaskHistories.AddRangeAsync(historyEntries);
            }

            await _context.SaveChangesAsync();

            return updatedTask == null ? null : MapToDto(updatedTask);
        }


        public async Task<DeleteTaskItemResponse> DeleteAsync(int id)
        {
            var ipAddress = GetIpAddress();

            try
            {
                _logger.LogInformation(
                    "Attempting to delete task {TaskId} from IP {IpAddress}",
                    id, ipAddress);

                var taskToDelete = await _repository.GetByIdAsync(id);
                if (taskToDelete == null)
                {
                    _logger.LogWarning(
                        "Delete failed - task {TaskId} not found from IP {IpAddress}",
                        id, ipAddress);

                    return new DeleteTaskItemResponse
                    {
                        Id = id,
                        Success = false,
                        Message = $"Task with ID {id} not found"
                    };
                }

                var deleted = await _repository.DeleteAsync(id);

                if (deleted)
                {
                    _logger.LogWarning(
                        "Task {TaskId} deleted. Title: {Title}, ProjectId: {ProjectId}, IP: {IpAddress}",
                        id, taskToDelete.Title, taskToDelete.ProjectId, ipAddress);
                }
                else
                {
                    _logger.LogError(
                        "Failed to delete task {TaskId} from IP {IpAddress}",
                        id, ipAddress);
                }

                return new DeleteTaskItemResponse
                {
                    Id = taskToDelete.Id,
                    Title = taskToDelete.Title,
                    Success = deleted,
                    Message = deleted ? "Task deleted successfully" : "Failed to delete task"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Unexpected error deleting task {TaskId} from IP {IpAddress}",
                    id, ipAddress);
                throw;
            }
        }

        [NoIntercept]
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
                ProjectId = task.ProjectId,
                ExecutorId = task.ExecutorId,
                CreatedDate = task.CreatedDate,
                Version = task.Version,
                DueDate = task.DueDate,

                Comments = task.Comments?.Select(c => new CommentDto
                {
                    Id = c.Id,
                    Text = c.Text,
                    CreatedDate = c.CreatedDate
                }).ToList() ?? new List<CommentDto>()
            };
        }

        [Cache(DurationSeconds = 60)]
        public async Task<PagedResult<TaskItemDto>> GetFilteredAsync(TaskItemFilterQuery query)
        {
            var result = await _repository.GetFilteredAsync(query);

            var dtoItems = result.Items.Select(t => new TaskItemDto
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                Status = t.Status.ToString(),
                Priority = t.Priority.ToString(),
                ProjectId = t.ProjectId,
                ExecutorId = t.ExecutorId,
                DueDate = t.DueDate,
                CreatedDate = t.CreatedDate,
                Version = t.Version,
                Comments = t.Comments?.Select(c => new CommentDto
                {
                    Id = c.Id,
                    Text = c.Text,
                    CreatedDate = c.CreatedDate,
                    TaskItemId = c.TaskItemId
                }).ToList() ?? new List<CommentDto>()
            }).ToList();

            return new PagedResult<TaskItemDto>
            {
                Items = dtoItems,
                TotalCount = result.TotalCount,
                PageNumber = result.PageNumber,
                PageSize = result.PageSize
            };
        }
        public async Task<BulkOperationResult> BulkDeleteAsync(List<int> ids)
        {
            var result = new BulkOperationResult { TotalProcessed = ids.Count };

            foreach (var id in ids)
            {
                try
                {
                    var deleted = await _repository.DeleteAsync(id);
                    if (deleted) result.SuccessCount++;
                    else result.Errors.Add(new BulkOperationError
                    {
                        TaskId = id, 
                        ErrorMessage = $"Завдання з ID {id} не знайдено"
                    });
                }
                catch (Exception ex)
                {
                    result.Errors.Add(new BulkOperationError
                    {
                        TaskId = id,
                        ErrorMessage = $"Помилка при видаленні ID {id}"
                    });
                }
            }

            return result;
        }
        public async Task<BulkOperationResult> BulkUpdateStatusAsync(BulkUpdateStatusCommand command)
        {
            var result = new BulkOperationResult { TotalProcessed = command.TaskIds.Count };

            foreach (var id in command.TaskIds)
            {
                try
                {
                    var task = await _repository.GetByIdAsync(id);
                    if (task == null)
                    {
                        result.Errors.Add(new BulkOperationError
                        {
                            TaskId = id,
                            ErrorMessage = $"Завдання з ID {id} не знайдено"
                        });
                        continue;
                    }

                    var oldStatus = task.Status.ToString();

                    if (Enum.TryParse<Models.TaskStatus>(command.NewStatus, true, out var parsedStatus))
                    {
                        // Оновлюємо задачу
                        task.Status = parsedStatus;
                        task.ModifiedDate = DateTime.UtcNow;
                        task.Version++;

                        await _repository.UpdateAsync(task);
                        result.SuccessCount++;

                        // Записуємо в історію (Audit Trail)
                        await _context.TaskHistories.AddAsync(new TaskHistory
                        {
                            TaskItemId = task.Id,
                            ChangeType = "BulkUpdate",
                            OldValue = oldStatus,
                            NewValue = command.NewStatus,
                            ChangedBy = "System",
                            Comment = "Масове оновлення статусу"
                        });
                    }
                    else
                    {
                        result.Errors.Add(new BulkOperationError
                        {
                            TaskId = id,
                            ErrorMessage = $"Некоретний статус {command.NewStatus} завдання ID {id}"
                        });
                    }
                }
                catch (Exception ex)
                {
                    result.Errors.Add(new BulkOperationError
                    {
                        TaskId = id,
                        ErrorMessage = $"Помилка при оновленні ID {id}"
                    });
                }
            }

            // Зберігаємо всі записи історії одним махом
            await _context.SaveChangesAsync();
            return result;
        }

        [Cache(DurationSeconds = 60)]
        public async Task<TaskStatisticsDto> GetStatisticsAsync()
        {
            var allTasks = await _repository.GetAllAsync();

            var total = allTasks.Count;
            var pending = allTasks.Count(t => t.Status == Models.TaskStatus.Pending);
            var inProgress = allTasks.Count(t => t.Status == Models.TaskStatus.InProgress);
            var done = allTasks.Count(t => t.Status == Models.TaskStatus.Done);

            var overdue = allTasks.Count(t =>
                t.Status != Models.TaskStatus.Done &&
                t.DueDate < DateTime.UtcNow);

            // Середній час виконання — тільки для завдань зі статусом Done і заповненим CompletedAt
            var completedTasks = allTasks
                .Where(t => t.Status == Models.TaskStatus.Done && t.CompletedAt.HasValue)
                .ToList();

            double avgCompletionHours = 0;
            if (completedTasks.Any())
            {
                avgCompletionHours = completedTasks
                    .Average(t => (t.CompletedAt!.Value - t.CreatedDate).TotalHours);
                avgCompletionHours = Math.Round(avgCompletionHours, 2);
            }

            return new TaskStatisticsDto
            {
                TotalTasks = total,
                PendingTasks = pending,
                InProgressTasks = inProgress,
                DoneTasks = done,
                OverdueTasks = overdue,
                CompletionRate = total > 0 ? Math.Round((double)done / total * 100, 2) : 0,
                AverageCompletionTime = avgCompletionHours
            };
        }

        [Cache(DurationSeconds = 60)]
        public async Task<List<PriorityDistributionDto>> GetPriorityDistributionAsync()
        {
            var allTasks = await _repository.GetAllAsync();

            return allTasks
                .GroupBy(t => t.Priority)
                .Select(g => new PriorityDistributionDto
                {
                    Priority = g.Key.ToString(),
                    Count = g.Count()
                })
                .OrderByDescending(d => d.Count)
                .ToList();
        }

        public async Task<List<TaskHistory>> GetHistoryAsync(int taskId)
        {
            return await _context.TaskHistories
                .Where(h => h.TaskItemId == taskId)
                .OrderByDescending(h => h.ChangeDate)
                .ToListAsync();
        }
        /// <summary>
        /// Отримати завдання за ID користувача
        /// </summary>
        public async Task<List<TaskItemDto>> GetTasksByUserAsync(int userId)
        {
            var ipAddress = GetIpAddress();

            using (_logger.BeginScope(new Dictionary<string, object>
            {
                ["UserId"] = userId,
                ["IpAddress"] = ipAddress,
                ["Operation"] = "GetTasksByUser"
            }))
            {
                try
                {
                    _logger.LogInformation("Fetching tasks for user");

                    var tasks = await _repository.GetByUserIdAsync(userId);

                    _logger.LogInformation(
                        "Retrieved {TaskCount} tasks for user",
                        tasks.Count);

                    return tasks.Select(MapToDto).ToList();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Unexpected error fetching tasks for user {UserId}",
                        userId);
                    throw;
                }
            }
        }
    }
}