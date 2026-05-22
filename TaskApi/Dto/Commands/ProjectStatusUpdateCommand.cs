namespace TaskApi.Dto.Commands
{
    public class ProjectStatusUpdateCommand
    {
        public string NewStatus { get; set; } = string.Empty;
        public string? Reason { get; set; }
    }
}