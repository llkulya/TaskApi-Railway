namespace TaskApi.Models
{
    public class Comment
    {
        public int Id { get; set; }
        public string Text { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public int TaskItemId { get; set; }
        public TaskItem? TaskItem { get; set; }
    }
}