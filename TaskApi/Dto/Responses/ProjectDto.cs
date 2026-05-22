namespace TaskApi.Dto.Responses
{
    public class ProjectDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int ProjectManagerId { get; set; }
        public string ManagerFullName { get; set; } = string.Empty;
    }
}