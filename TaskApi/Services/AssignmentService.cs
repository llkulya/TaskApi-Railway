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

        public AssignmentService(ITaskRepository taskRepository, IExecutorRepository executorRepository)
        {
            _taskRepository = taskRepository;
            _executorRepository = executorRepository;
        }

        /// <summary>
        /// Юз-кейс 4.1 — Призначення виконавця на завдання
        /// </summary>
        public async Task<bool> AssignTaskToExecutorAsync(AssignExecutorCommand command)
        {
            // Перевірка чи завдання існує
            var task = await _taskRepository.GetByIdAsync(command.TaskId);
            if (task == null)
                throw new KeyNotFoundException($"Завдання з ID {command.TaskId} не знайдено");

            // Перевірка чи виконавець існує
            var executor = await _executorRepository.GetByIdAsync(command.ExecutorId);
            if (executor == null)
                throw new KeyNotFoundException($"Виконавець з ID {command.ExecutorId} не знайдено");

            // Перевірка чи завдання не в статусі Done
            if (task.Status == TaskStatus.Done)
                throw new InvalidOperationException(
                    "Не можна призначити виконавця на завершене завдання");

            // Оптимістичне блокування
            if (task.Version != command.Version)
                throw new ConcurrencyException(
                    $"Завдання було змінено. Поточна версія: {task.Version}, ваша версія: {command.Version}");

            // Оновлення
            task.ExecutorId = command.ExecutorId;
            task.ModifiedDate = DateTime.UtcNow;
            task.Version++;

            await _taskRepository.UpdateAsync(task);
            return true;
        }

        /// <summary>
        /// Юз-кейс 4.2 — Зміна виконавця завдання
        /// </summary>
        public async Task<bool> ChangeExecutorAsync(ChangeExecutorCommand command)
        {
            // Перевірка чи завдання існує
            var task = await _taskRepository.GetByIdAsync(command.TaskId);
            if (task == null)
                throw new KeyNotFoundException($"Завдання з ID {command.TaskId} не знайдено");

            // Оптимістичне блокування
            if (task.Version != command.Version)
                throw new ConcurrencyException(
                    $"Завдання було змінено. Поточна версія: {task.Version}, ваша версія: {command.Version}");

            // Перевірка чи новий виконавець існує
            var newExecutor = await _executorRepository.GetByIdAsync(command.NewExecutorId);
            if (newExecutor == null)
                throw new KeyNotFoundException($"Виконавець з ID {command.NewExecutorId} не знайдено");

            // Перевірка чи новий виконавець відрізняється від поточного
            if (task.ExecutorId == command.NewExecutorId)
                throw new InvalidOperationException(
                    "Новий виконавець не може бути тим самим що і поточний");

            // Оновлення
            task.ExecutorId = command.NewExecutorId;
            task.ModifiedDate = DateTime.UtcNow;
            task.Version++;

            await _taskRepository.UpdateAsync(task);
            return true;
        }
    }
}