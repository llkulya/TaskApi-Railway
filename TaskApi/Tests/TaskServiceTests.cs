using Moq;
using NUnit.Framework;
using TaskApi.Dto.Commands;
using TaskApi.Dto.Queries;
using TaskApi.Models;
using TaskApi.Repositories;
using TaskApi.Services;
using TaskApi.Exceptions;
using System.ComponentModel.DataAnnotations;

namespace TaskApi.Tests
{
    [TestFixture]
    public class TaskServiceTests
    {
        private Mock<ITaskRepository> _mockRepository;
        private ITaskService _service;

        [SetUp]
        public void Setup()
        {
            _mockRepository = new Mock<ITaskRepository>();
            _service = new TaskService(_mockRepository.Object);
        }

        // Тест 1: Створення завдання
        [Test]
        public async Task CreateAsync_ShouldAssignCreatedDate()
        {
            var dto = new TaskItemCreateCommand
            {
                Title = "Нове завдання",
                Description = "Опис",
                Status = "Pending",
                Priority = "High"
            };

            var createdTask = new TaskItem
            {
                Id = 1,
                Title = dto.Title,
                Description = dto.Description,
                Status = Models.TaskStatus.Pending,
                Priority = Models.TaskPriority.High,
                CreatedDate = DateTime.UtcNow
            };

            _mockRepository.Setup(r => r.AddAsync(It.IsAny<TaskItem>()))
                           .ReturnsAsync(createdTask);

            var result = await _service.CreateAsync(dto);

            Assert.That(result.CreatedDate, Is.Not.EqualTo(default(DateTime)));
            Assert.That(result.Title, Is.EqualTo("Нове завдання"));
            Assert.That(result.Status, Is.EqualTo("Pending"));
        }

        // Тест 2: Валідація порожнього заголовка
        [Test]
        public void CreateAsync_ShouldThrowException_WhenTitleIsEmpty()
        {
            var dto = new TaskItemCreateCommand
            {
                Title = "",
                Description = "Опис"
            };

            Assert.ThrowsAsync<ValidationException>(
                async () => await _service.CreateAsync(dto));
        }

        // Тест 3: Заборона зміни статусу з Done на InProgress
        [Test]
        public void UpdateAsync_ShouldPreventTransitionFromDoneToInProgress()
        {
            var existingTask = new TaskItem
            {
                Id = 1,
                Status = Models.TaskStatus.Done,
                Version = 1
            };

            _mockRepository.Setup(r => r.GetByIdAsync(1))
                           .ReturnsAsync(existingTask);

            var updateCommand = new TaskItemUpdateCommand
            {
                Id = 1,
                Status = "InProgress",
                Version = 1
            };

            Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _service.UpdateAsync(updateCommand));
        }

        // Тест 4: Успішне оновлення статусу
        [Test]
        public async Task UpdateAsync_ShouldUpdateTask_WhenValidStatus()
        {
            var existingTask = new TaskItem
            {
                Id = 1,
                Status = Models.TaskStatus.Pending,
                Priority = Models.TaskPriority.Medium,
                Version = 1
            };

            _mockRepository.Setup(r => r.GetByIdAsync(1))
                           .ReturnsAsync(existingTask);
            _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<TaskItem>()))
                           .ReturnsAsync((TaskItem t) => t);

            var updateCommand = new TaskItemUpdateCommand
            {
                Id = 1,
                Status = "InProgress",
                Priority = "High",
                Version = 1
            };

            var result = await _service.UpdateAsync(updateCommand);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Status, Is.EqualTo("InProgress"));
            Assert.That(result.Priority, Is.EqualTo("High"));
        }

        // Тест 5: Отримання завдань з високим пріоритетом
        [Test]
        public async Task GetHighPriorityAsync_ShouldReturnOnlyHighPriority()
        {
            var tasks = new List<TaskItem>
            {
                new TaskItem { Id = 1, Priority = Models.TaskPriority.High },
                new TaskItem { Id = 2, Priority = Models.TaskPriority.Critical },
                new TaskItem { Id = 3, Priority = Models.TaskPriority.Low }
            };

            _mockRepository.Setup(r => r.GetHighPriorityAsync())
                .ReturnsAsync(tasks.Where(t =>
                    t.Priority == Models.TaskPriority.High ||
                    t.Priority == Models.TaskPriority.Critical).ToList());

            var result = await _service.GetHighPriorityAsync();

            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result.All(t =>
                t.Priority == "High" || t.Priority == "Critical"), Is.True);
        }

        // Тест 6: Завдання не знайдено
        [Test]
        public async Task GetByIdAsync_ShouldReturnNull_WhenTaskNotFound()
        {
            _mockRepository.Setup(r => r.GetByIdAsync(999))
                           .ReturnsAsync((TaskItem?)null);

            var result = await _service.GetByIdAsync(999);

            Assert.That(result, Is.Null);
        }

        // Тест 7: Видалення завдання
        [Test]
        public async Task DeleteAsync_ShouldReturnTrue_WhenTaskExists()
        {
            var existingTask = new TaskItem { Id = 1, Title = "Завдання" };
            _mockRepository.Setup(r => r.GetByIdAsync(1))
                           .ReturnsAsync(existingTask);
            _mockRepository.Setup(r => r.DeleteAsync(1))
                           .ReturnsAsync(true);

            var result = await _service.DeleteAsync(1);

            Assert.That(result.Success, Is.True);
            Assert.That(result.Message, Is.EqualTo("Task deleted successfully"));
        }

        // Тест 8: Мапінг моделі на DTO
        [Test]
        public async Task GetAllAsync_ShouldMapToDtoCorrectly()
        {
            var tasks = new List<TaskItem>
            {
                new TaskItem
                {
                    Id = 1,
                    Title = "Тест",
                    Status = Models.TaskStatus.Done,
                    Priority = Models.TaskPriority.High,
                    CreatedDate = DateTime.UtcNow
                }
            };

            _mockRepository.Setup(r => r.GetAllAsync())
                           .ReturnsAsync(tasks);

            var result = await _service.GetAllAsync();

            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].Title, Is.EqualTo("Тест"));
            Assert.That(result[0].Status, Is.EqualTo("Done"));
            Assert.That(result[0].Priority, Is.EqualTo("High"));
        }

        // Тест 9: Призначення виконавця на Done завдання
        [Test]
        public void AssignTaskToExecutorAsync_ShouldThrow_WhenTaskIsDone()
        {
            var mockExecutorRepo = new Mock<IExecutorRepository>();
            var assignmentService = new AssignmentService(
                _mockRepository.Object,
                mockExecutorRepo.Object);

            var existingTask = new TaskItem
            {
                Id = 1,
                Status = Models.TaskStatus.Done,
                Version = 0
            };

            var executor = new Executor
            {
                Id = 1,
                FirstName = "Тест",
                LastName = "Тестовий"
            };

            _mockRepository.Setup(r => r.GetByIdAsync(1))
                           .ReturnsAsync(existingTask);
            mockExecutorRepo.Setup(r => r.GetByIdAsync(1))
                            .ReturnsAsync(executor);

            var command = new AssignExecutorCommand
            {
                TaskId = 1,
                ExecutorId = 1,
                Version = 0
            };

            Assert.ThrowsAsync<InvalidOperationException>(
                async () => await assignmentService.AssignTaskToExecutorAsync(command));
        }

        // Тест 10: Зміна виконавця на того самого
        [Test]
        public void ChangeExecutorAsync_ShouldThrow_WhenSameExecutor()
        {
            var mockExecutorRepo = new Mock<IExecutorRepository>();
            var assignmentService = new AssignmentService(
                _mockRepository.Object,
                mockExecutorRepo.Object);

            var existingTask = new TaskItem
            {
                Id = 1,
                Status = Models.TaskStatus.Pending,
                ExecutorId = 5,
                Version = 0
            };

            var executor = new Executor
            {
                Id = 5,
                FirstName = "Тест",
                LastName = "Тестовий"
            };

            _mockRepository.Setup(r => r.GetByIdAsync(1))
                           .ReturnsAsync(existingTask);
            mockExecutorRepo.Setup(r => r.GetByIdAsync(5))
                            .ReturnsAsync(executor);

            var command = new ChangeExecutorCommand
            {
                TaskId = 1,
                NewExecutorId = 5,
                Version = 0
            };

            Assert.ThrowsAsync<InvalidOperationException>(
                async () => await assignmentService.ChangeExecutorAsync(command));
        }
    }
}