namespace TaskApi.Dto.Responses
{
    public class TaskItemDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public int? ProjectId { get; set; }
        public int? ExecutorId { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime CreatedDate { get; set; }
        public int Version { get; set; }
        public List<CommentDto> Comments { get; set; } = new();
    }
}