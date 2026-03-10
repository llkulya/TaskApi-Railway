namespace TaskApi.Dto.Responses
{
    public class DeleteTaskItemResponse
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}