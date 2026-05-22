namespace TaskApi.Models
{
    public class TaskHistory
    {
        public int Id { get; set; }
        public int TaskItemId { get; set; } // Зв'язок із завданням
        public DateTime ChangeDate { get; set; } = DateTime.UtcNow;
        public string ChangeType { get; set; } = string.Empty; // Наприклад: "StatusChanged", "Updated"
        public string OldValue { get; set; } = string.Empty;
        public string NewValue { get; set; } = string.Empty;
        public string ChangedBy { get; set; } = "system";
        public string? Comment { get; set; }

        // Навігаційна властивість
        public TaskItem? Task { get; set; }
    }
}