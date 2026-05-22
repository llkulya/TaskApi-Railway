using TaskApi.Dto.Commands;
using TaskApi.Exceptions;
using TaskApi.Repositories;
using TaskStatus = TaskApi.Models.TaskStatus;

namespace TaskApi.Services
{
    public class AssignmentService : IAssignmentService
    {
        private readonly ITaskRepository _taskRepository;
        private readonly IExecutorRepository _executorRepository;
        private readonly ILogger<AssignmentService> _logger;

        public AssignmentService(
            ITaskRepository taskRepository,
            IExecutorRepository executorRepository,
            ILogger<AssignmentService> logger)
        {
            _taskRepository = taskRepository;
            _executorRepository = executorRepository;
            _logger = logger;
        }

        /// <summary>
        /// Юз-кейс 4.1 — Призначення виконавця на завдання
        /// </summary>
        public async Task<bool> AssignTaskToExecutorAsync(AssignExecutorCommand command)
        {
            var task = await _taskRepository.GetByIdAsync(command.TaskId);
            if (task == null)
                throw new KeyNotFoundException($"Завдання з ID {command.TaskId} не знайдено");

            var executor = await _executorRepository.GetByIdAsync(command.ExecutorId);
            if (executor == null)
                throw new KeyNotFoundException($"Виконавець з ID {command.ExecutorId} не знайдено");

            if (task.Status == TaskStatus.Done)
                throw new InvalidOperationException(
                    "Не можна призначити виконавця на завершене завдання");

            if (task.Version != command.Version)
                throw new ConcurrencyException(
                    $"Завдання було змінено. Поточна версія: {task.Version}, ваша версія: {command.Version}");

            var oldExecutorId = task.ExecutorId;

            task.ExecutorId = command.ExecutorId;
            task.ModifiedDate = DateTime.UtcNow;
            task.Version++;

            await _taskRepository.UpdateAsync(task);

            _logger.LogInformation(
                "Task {TaskId} assigned from executor {AssignedFrom} to executor {AssignedTo}",
                task.Id,
                oldExecutorId,
                command.ExecutorId);

            return true;
        }

        /// <summary>
        /// Юз-кейс 4.2 — Зміна виконавця завдання
        /// </summary>
        public async Task<bool> ChangeExecutorAsync(ChangeExecutorCommand command)
        {
            var task = await _taskRepository.GetByIdAsync(command.TaskId);
            if (task == null)
                throw new KeyNotFoundException($"Завдання з ID {command.TaskId} не знайдено");

            if (task.Version != command.Version)
                throw new ConcurrencyException(
                    $"Завдання було змінено. Поточна версія: {task.Version}, ваша версія: {command.Version}");

            var newExecutor = await _executorRepository.GetByIdAsync(command.NewExecutorId);
            if (newExecutor == null)
                throw new KeyNotFoundException($"Виконавець з ID {command.NewExecutorId} не знайдено");

            if (task.ExecutorId == command.NewExecutorId)
                throw new InvalidOperationException(
                    "Новий виконавець не може бути тим самим що і поточний");

            var oldExecutorId = task.ExecutorId;

            task.ExecutorId = command.NewExecutorId;
            task.ModifiedDate = DateTime.UtcNow;
            task.Version++;

            await _taskRepository.UpdateAsync(task);

            _logger.LogInformation(
                "Task {TaskId} executor changed from {AssignedFrom} to {AssignedTo}. Reason: {Reason}",
                task.Id,
                oldExecutorId,
                command.NewExecutorId,
                command.Reason ?? "not specified");

            return true;
        }
    }
}