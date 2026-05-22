namespace TaskApi.Dto.Responses
{
    public class MetricsDto
    {
        public long TotalRequests { get; set; }
        public long TotalErrors { get; set; }
        public double AverageDurationMs { get; set; }

        public Dictionary<string, long> RequestsByEndpoint { get; set; } = new();
        public Dictionary<string, long> ErrorsByEndpoint { get; set; } = new();
    }
}