namespace TaskApi.Dto.Responses
{
    public class BulkOperationResult
    {
        public int TotalProcessed { get; set; }
        public int SuccessCount { get; set; }
        public int FailedCount => Errors.Count;

        public List<BulkOperationError> Errors { get; set; } = new();

        public bool IsSuccess => Errors.Count == 0;
    }

    public class BulkOperationError
    {
        public int TaskId { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }
}