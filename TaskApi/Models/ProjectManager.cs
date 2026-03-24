namespace TaskApi.Models
{
    public class ProjectManager
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public List<Project> ManagedProjects { get; set; } = new();
    }
}