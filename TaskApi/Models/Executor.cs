namespace TaskApi.Models
{
    public class Executor
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime HireDate { get; set; }
        public ExecutorRole Role { get; set; }
        public List<TaskItem> AssignedTasks { get; set; } = new();
    }

    public enum ExecutorRole
    {
        Developer,
        Tester,
        Designer,
        Manager
    }
}