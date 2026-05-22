using System.Collections.Concurrent;
using TaskApi.Dto.Responses;

namespace TaskApi.Services
{
    public class MetricsService : IMetricsService
    {
        private long _totalRequests;
        private long _totalErrors;
        private long _totalDurationMs;

        private readonly ConcurrentDictionary<string, long> _requestsByEndpoint = new();
        private readonly ConcurrentDictionary<string, long> _errorsByEndpoint = new();

        public void RecordRequest(string method, string path, int statusCode, long durationMs)
        {
            var endpoint = $"{method} {path}";

            Interlocked.Increment(ref _totalRequests);
            Interlocked.Add(ref _totalDurationMs, durationMs);

            _requestsByEndpoint.AddOrUpdate(endpoint, 1, (_, count) => count + 1);

            if (statusCode >= 400)
            {
                Interlocked.Increment(ref _totalErrors);
                _errorsByEndpoint.AddOrUpdate(endpoint, 1, (_, count) => count + 1);
            }
        }

        public MetricsDto GetMetrics()
        {
            var totalRequests = Interlocked.Read(ref _totalRequests);
            var totalErrors = Interlocked.Read(ref _totalErrors);
            var totalDuration = Interlocked.Read(ref _totalDurationMs);

            return new MetricsDto
            {
                TotalRequests = totalRequests,
                TotalErrors = totalErrors,
                AverageDurationMs = totalRequests == 0
                    ? 0
                    : Math.Round((double)totalDuration / totalRequests, 2),

                RequestsByEndpoint = _requestsByEndpoint
                    .OrderByDescending(x => x.Value)
                    .ToDictionary(x => x.Key, x => x.Value),

                ErrorsByEndpoint = _errorsByEndpoint
                    .OrderByDescending(x => x.Value)
                    .ToDictionary(x => x.Key, x => x.Value)
            };
        }
    }
}