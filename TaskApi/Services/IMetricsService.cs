using TaskApi.Dto.Responses;

namespace TaskApi.Services
{
    public interface IMetricsService
    {
        void RecordRequest(string method, string path, int statusCode, long durationMs);
        MetricsDto GetMetrics();
    }
}