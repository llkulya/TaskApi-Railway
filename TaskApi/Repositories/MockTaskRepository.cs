/*using System.Collections.Concurrent;
using TaskApi.Models;

namespace TaskApi.Repositories
{
    public class MockTaskRepository : ITaskRepository
    {
        private readonly ConcurrentDictionary<int, TaskItem> _tasks = new();
        private int _nextId = 1;

        public MockTaskRepository()
        {
            InitializeTestData();
        }

        private void InitializeTestData()
        {
            var testData = new List<TaskItem>
            {
                new TaskItem
                {
                    Id = 1,
                    Title = "Завдання 1",
                    Description = "Опис завдання 1",
                    Status = Models.TaskStatus.Pending,
                    Priority = Models.TaskPriority.Medium,
                    CreatedDate = DateTime.UtcNow.AddDays(-2)
                },
                new TaskItem
                {
                    Id = 2,
                    Title = "Завдання 2",
                    Description = "Опис завдання 2",
                    Status = Models.TaskStatus.InProgress,
                    Priority = Models.TaskPriority.High,
                    CreatedDate = DateTime.UtcNow.AddDays(-1)
                },
                new TaskItem
                {
                    Id = 3,
                    Title = "Завдання 3",
                    Description = "Опис завдання 3",
                    Status = Models.TaskStatus.Done,
                    Priority = Models.TaskPriority.Low,
                    CreatedDate = DateTime.UtcNow
                }
            };

            foreach (var task in testData)
            {
                _tasks[task.Id] = task;
                _nextId = Math.Max(_nextId, task.Id + 1);
            }
        }

        public Task<List<TaskItem>> GetAllAsync()
        {
            return Task.FromResult(_tasks.Values.ToList());
        }

        public Task<TaskItem?> GetByIdAsync(int id)
        {
            _tasks.TryGetValue(id, out var task);
            return Task.FromResult<TaskItem?>(task);
        }

        public Task<TaskItem> AddAsync(TaskItem item)
        {
            item.Id = _nextId++;
            item.CreatedDate = DateTime.UtcNow;
            _tasks[item.Id] = item;
            return Task.FromResult(item);
        }

        public Task<TaskItem?> UpdateAsync(TaskItem item)
        {
            if (!_tasks.ContainsKey(item.Id))
                return Task.FromResult<TaskItem?>(null);

            item.ModifiedDate = DateTime.UtcNow;
            _tasks[item.Id] = item;
            return Task.FromResult<TaskItem?>(item);
        }

        public Task<bool> DeleteAsync(int id)
        {
            return Task.FromResult(_tasks.TryRemove(id, out _));
        }

        public Task<List<TaskItem>> GetByStatusAsync(Models.TaskStatus status)
        {
            var result = _tasks.Values
                .Where(t => t.Status == status)
                .ToList();
            return Task.FromResult(result);
        }

        public Task<List<TaskItem>> GetHighPriorityAsync()
        {
            var result = _tasks.Values
                .Where(t => t.Priority == Models.TaskPriority.High || t.Priority == Models.TaskPriority.Critical)
                .ToList();
            return Task.FromResult(result);
        }
    }
}*/