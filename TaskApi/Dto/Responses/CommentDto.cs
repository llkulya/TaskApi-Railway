namespace TaskApi.Dto.Responses
{
    public class CommentDto
    {
        public int Id { get; set; }
        public string Text { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public int TaskItemId { get; set; }
    }
}