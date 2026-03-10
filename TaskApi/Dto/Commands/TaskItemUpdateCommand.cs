namespace TaskApi.Dto.Commands
{
    public class TaskItemUpdateCommand
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Status { get; set; }
        public string? Priority { get; set; }
        public int Version { get; set; } 
    }
}
