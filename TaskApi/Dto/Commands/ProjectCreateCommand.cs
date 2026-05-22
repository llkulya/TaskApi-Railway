namespace TaskApi.Dto.Commands
{
    public class ProjectCreateCommand
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int ProjectManagerId { get; set; }
    }
}