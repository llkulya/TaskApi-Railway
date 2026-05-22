namespace TaskApi.Models
{
    public class TaskItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public TaskStatus Status { get; set; } = TaskStatus.Pending;
        public TaskPriority Priority { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int Version { get; set; }

        /// <summary>
        /// ID проєкту, до якого належить завдання
        /// </summary>
        public int? ProjectId { get; set; }
        public Project? Project { get; set; }

        /// <summary>
        /// ID виконавця, призначеного на завдання
        /// </summary>
        public int? ExecutorId { get; set; }
        public Executor? Executor { get; set; }
        public List<Comment> Comments { get; set; } = new();
    }

    public enum TaskPriority
    {
        Low,
        Medium,
        High,
        Critical
    }

    public enum TaskStatus
    {
        Pending,
        InProgress,
        Done
    }
}