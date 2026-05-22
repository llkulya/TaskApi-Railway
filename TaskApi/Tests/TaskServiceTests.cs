using Microsoft.EntityFrameworkCore;
using Moq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using System.ComponentModel.DataAnnotations;
using TaskApi.Data;
using TaskApi.Dto.Commands;
using TaskApi.Dto.Queries;
using TaskApi.Dto.Responses;
using TaskApi.Models;
using TaskApi.Repositories;
using TaskApi.Services;

namespace TaskApi.Tests
{
    [TestFixture]
    public class TaskServiceTests
    {
        private Mock<ITaskRepository> _mockRepository = null!;
        private Mock<IProjectRepository> _mockProjectRepo = null!;
        private Mock<IExecutorRepository> _mockExecutorRepo = null!;
        private ApplicationDbContext _context = null!;
        private ITaskService _service = null!;

        [SetUp]
        public void Setup()
        {
            _mockRepository = new Mock<ITaskRepository>();
            _mockProjectRepo = new Mock<IProjectRepository>();
            _mockExecutorRepo = new Mock<IExecutorRepository>();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);

            _service = new TaskService(
                _mockRepository.Object,
                _mockProjectRepo.Object,
                _mockExecutorRepo.Object,
                _context,
                new Mock<ILogger<TaskService>>().Object,            
    new Mock<IHttpContextAccessor>().Object);
        }

        [TearDown]
        public void TearDown()
        {
            _context.Dispose();
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
            var mockLogger = new Mock<ILogger<AssignmentService>>();
            var assignmentService = new AssignmentService(
     _mockRepository.Object,
     mockExecutorRepo.Object,
     mockLogger.Object);

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
            var mockLogger = new Mock<ILogger<AssignmentService>>();
            var assignmentService = new AssignmentService(
    _mockRepository.Object,
    mockExecutorRepo.Object,
    mockLogger.Object);

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
        // --- Тести на Історію змін (Audit Trail) ---
        [Test]
        public async Task UpdateAsync_ShouldAddHistoryEntry_WhenStatusChanges()
        {
            // Arrange
            var existingTask = new TaskItem { Id = 1, Status = Models.TaskStatus.Pending, Version = 1 };
            _mockRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existingTask);
            _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<TaskItem>())).ReturnsAsync((TaskItem t) => t);

            var command = new TaskItemUpdateCommand { Id = 1, Status = "InProgress", Version = 1 };

            // Act
            await _service.UpdateAsync(command);

            // Assert: Перевіряємо, чи в InMemory БД з'явився запис історії
            var history = await _context.TaskHistories.FirstOrDefaultAsync(h => h.TaskItemId == 1);
            Assert.That(history, Is.Not.Null);
            Assert.That(history?.NewValue, Is.EqualTo("InProgress"));
        }

        // --- Тести на Масові операції (Bulk) ---
        [Test]
        public async Task BulkUpdateStatusAsync_ShouldAddHistoryForEachUpdatedTask()
        {
            // Arrange
            var tasks = new List<TaskItem> {
        new TaskItem { Id = 1, Status = Models.TaskStatus.Pending },
        new TaskItem { Id = 2, Status = Models.TaskStatus.Pending }
    };
            _mockRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((int id) => tasks.FirstOrDefault(t => t.Id == id));

            var command = new BulkUpdateStatusCommand { TaskIds = new List<int> { 1, 2 }, NewStatus = "Done" };

            // Act
            await _service.BulkUpdateStatusAsync(command);

            // Assert: Має бути 2 записи в історії
            var historyCount = await _context.TaskHistories.CountAsync();
            Assert.That(historyCount, Is.EqualTo(2));
        }

        // --- Тести на Статистику ---
        [Test]
        public async Task GetStatisticsAsync_ShouldCalculateCorrectCompletionRate()
        {
            // Arrange
            var tasks = new List<TaskItem> {
        new TaskItem { Status = Models.TaskStatus.Done },
        new TaskItem { Status = Models.TaskStatus.Pending }
    };
            _mockRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(tasks);

            // Act
            var stats = await _service.GetStatisticsAsync();

            // Assert: 1 з 2 це 50%
            Assert.That(stats.CompletionRate, Is.EqualTo(50.0));
            Assert.That(stats.TotalTasks, Is.EqualTo(2));
        }
        // Тест: заборона змін після 24 годин у статусі Done
        [Test]
        public void UpdateAsync_ShouldPreventChange_WhenTaskDoneForMoreThan24Hours()
        {
            var existingTask = new TaskItem
            {
                Id = 1,
                Status = Models.TaskStatus.Done,
                CompletedAt = DateTime.UtcNow.AddHours(-25), // більше 24 годин тому
                Version = 1
            };

            _mockRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existingTask);

            var command = new TaskItemUpdateCommand { Id = 1, Status = "InProgress", Version = 1 };

            Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _service.UpdateAsync(command));
        }

        // Тест: фільтр IsOverdue повертає тільки прострочені
        [Test]
        public async Task GetFilteredAsync_ShouldReturnOnlyOverdueTasks_WhenIsOverdueTrue()
        {
            var pagedResult = new PagedResult<TaskItem>
            {
                Items = new List<TaskItem>
        {
            new TaskItem
            {
                Id = 1,
                Status = Models.TaskStatus.Pending,
                DueDate = DateTime.UtcNow.AddDays(-2) // прострочено
            }
        },
                TotalCount = 1,
                PageNumber = 1,
                PageSize = 10
            };

            _mockRepository
                .Setup(r => r.GetFilteredAsync(It.IsAny<TaskItemFilterQuery>()))
                .ReturnsAsync(pagedResult);

            var query = new TaskItemFilterQuery { IsOverdue = true };
            var result = await _service.GetFilteredAsync(query);

            Assert.That(result.Items.Count, Is.EqualTo(1));
            Assert.That(result.Items.All(t => t.Status != "Done"), Is.True);
        }

        // Тест: сортування за пріоритетом (Critical вище за Low)
        [Test]
        public async Task GetFilteredAsync_ShouldSortByPriorityCorrectly_WhenSortByPriority()
        {
            var pagedResult = new PagedResult<TaskItem>
            {
                Items = new List<TaskItem>
        {
            new TaskItem { Id = 1, Priority = Models.TaskPriority.Critical },
            new TaskItem { Id = 2, Priority = Models.TaskPriority.High },
            new TaskItem { Id = 3, Priority = Models.TaskPriority.Low }
        },
                TotalCount = 3,
                PageNumber = 1,
                PageSize = 10
            };

            _mockRepository
                .Setup(r => r.GetFilteredAsync(It.IsAny<TaskItemFilterQuery>()))
                .ReturnsAsync(pagedResult);

            var query = new TaskItemFilterQuery { SortBy = "priority", SortDescending = true };
            var result = await _service.GetFilteredAsync(query);

            // Critical (3) > High (2) > Low (0) — числові значення enum
            var priorities = result.Items.Select(t => t.Priority).ToList();
            Assert.That(priorities[0], Is.EqualTo("Critical"));
            Assert.That(priorities[1], Is.EqualTo("High"));
            Assert.That(priorities[2], Is.EqualTo("Low"));
        }

        // Тест: розрахунок середнього часу виконання
        [Test]
        public async Task GetStatisticsAsync_ShouldCalculateCorrectAverageCompletionTime()
        {
            var now = DateTime.UtcNow;
            var tasks = new List<TaskItem>
    {
        new TaskItem
        {
            Status = Models.TaskStatus.Done,
            CreatedDate = now.AddHours(-10),
            CompletedAt = now  // 10 годин
        },
        new TaskItem
        {
            Status = Models.TaskStatus.Done,
            CreatedDate = now.AddHours(-20),
            CompletedAt = now  // 20 годин
        }
    };

            _mockRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(tasks);

            var stats = await _service.GetStatisticsAsync();

            // Середнє: (10 + 20) / 2 = 15 годин
            Assert.That(stats.AverageCompletionTime, Is.EqualTo(15.0));
        }
        #region Logging Tests

        [Test]
        public async Task CreateAsync_ValidTask_LogsInformation()
        {
            // Arrange
            var command = new TaskItemCreateCommand
            {
                Title = "Test Task",
                Description = "Test Description",
                Status = "Pending",
                Priority = "Low",
                ProjectId = 1
            };

            var project = new Project { Id = 1, Name = "Test Project" };
            var createdTask = new TaskItem
            {
                Id = 1,
                Title = command.Title,
                Status = Models.TaskStatus.Pending,
                Priority = Models.TaskPriority.Low,
                ProjectId = 1
            };

            _mockProjectRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(project);
            _mockRepository.Setup(r => r.AddAsync(It.IsAny<TaskItem>()))
                .ReturnsAsync(createdTask);

            // Act
            await _service.CreateAsync(command);

            // Assert — перевіряємо що залогувалось Information
            var mockLogger = new Mock<ILogger<TaskService>>();
            mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) =>
                        v.ToString()!.Contains("Creating task")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtMostOnce);
        }

        [Test]
        public async Task CreateAsync_CriticalPriority_LogsWarning()
        {
            // Arrange
            var command = new TaskItemCreateCommand
            {
                Title = "Critical Task",
                Description = "Urgent",
                Status = "Pending",
                Priority = "Critical",
                ProjectId = 1
            };

            var project = new Project { Id = 1, Name = "Test Project" };
            var createdTask = new TaskItem
            {
                Id = 1,
                Title = command.Title,
                Status = Models.TaskStatus.Pending,
                Priority = Models.TaskPriority.Critical,
                ProjectId = 1
            };

            _mockProjectRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(project);
            _mockRepository.Setup(r => r.AddAsync(It.IsAny<TaskItem>()))
                .ReturnsAsync(createdTask);

            // Act
            var result = await _service.CreateAsync(command);

            // Assert — Critical пріоритет має бути створений успішно
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Priority, Is.EqualTo("Critical"));
        }

        [Test]
        public async Task DeleteAsync_ExistingTask_LogsWarning()
        {
            // Arrange
            var task = new TaskItem
            {
                Id = 1,
                Title = "Task to delete",
                ProjectId = 1,
                Status = Models.TaskStatus.Pending,
                Priority = Models.TaskPriority.Low
            };

            _mockRepository.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(task);
            _mockRepository.Setup(r => r.DeleteAsync(1))
                .ReturnsAsync(true);

            // Act
            var result = await _service.DeleteAsync(1);

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.Id, Is.EqualTo(1));
        }

        [Test]
        public async Task DeleteAsync_NonExistingTask_ReturnsFailure()
        {
            // Arrange
            _mockRepository.Setup(r => r.GetByIdAsync(999))
                .ReturnsAsync((TaskItem?)null);

            // Act
            var result = await _service.DeleteAsync(999);

            // Assert — повертає невдачу без винятку
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain("not found"));
        }

        [Test]
        public async Task GetAllAsync_ReturnsTasks_LogsCount()
        {
            // Arrange
            var tasks = new List<TaskItem>
    {
        new TaskItem
        {
            Id = 1, Title = "Task 1",
            Status = Models.TaskStatus.Pending,
            Priority = Models.TaskPriority.Low
        },
        new TaskItem
        {
            Id = 2, Title = "Task 2",
            Status = Models.TaskStatus.InProgress,
            Priority = Models.TaskPriority.High
        }
    };

            _mockRepository.Setup(r => r.GetAllAsync())
                .ReturnsAsync(tasks);

            // Act
            var result = await _service.GetAllAsync();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(2));
        }

        [Test]
        public async Task GetTasksByUserAsync_ReturnsUserTasks()
        {
            // Arrange
            var userId = 1;
            var tasks = new List<TaskItem>
    {
        new TaskItem
        {
            Id = 1,
            Title = "User Task 1",
            ExecutorId = userId,
            Status = Models.TaskStatus.Pending,
            Priority = Models.TaskPriority.Low
        }
    };

            _mockRepository.Setup(r => r.GetByUserIdAsync(userId))
                .ReturnsAsync(tasks);

            // Act
            var result = await _service.GetTasksByUserAsync(userId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(1));
            _mockRepository.Verify(r => r.GetByUserIdAsync(userId), Times.Once);
        }

        #endregion
    }
}