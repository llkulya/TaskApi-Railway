namespace TaskApi.Models
{
    public class TaskItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty; // Додано = string.Empty
        public Models.TaskStatus Status { get; set; }
        public Models.TaskPriority Priority { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public int Version { get; set; } 
    }

    public enum TaskPriority
    {
        Low, Medium, High, Critical
    }


    public enum TaskStatus
    {
        Pending, InProgress, Done
    }

}
