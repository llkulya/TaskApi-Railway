namespace TaskApi.Dto.Commands
{
    public class BulkDeleteCommand
    {
        public List<int> TaskIds { get; set; } = new();

        public string? Reason { get; set; }
        public string? DeletedBy { get; set; }
    }
}