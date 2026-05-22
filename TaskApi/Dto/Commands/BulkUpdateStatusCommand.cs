namespace TaskApi.Dto.Commands
{
    public class BulkUpdateStatusCommand
    {
        public List<int> TaskIds { get; set; } = new();
        public string NewStatus { get; set; } = string.Empty;
    }
}