namespace TaskApi.Models
{
    public class Project
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public ProjectStatus Status { get; set; } = ProjectStatus.Active;
        public int ManagerId { get; set; }
        public ProjectManager? Manager { get; set; }
        public List<TaskItem> Tasks { get; set; } = new();
    }

    public enum ProjectStatus
    {
        Active,
        Completed,
        OnHold,
        Cancelled
    }
}