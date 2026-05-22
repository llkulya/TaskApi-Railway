using System.Diagnostics;
using TaskApi.Services;

namespace TaskApi.Middleware
{
    public class MetricsMiddleware
    {
        private readonly RequestDelegate _next;

        public MetricsMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IMetricsService metricsService)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                await _next(context);
            }
            finally
            {
                stopwatch.Stop();

                metricsService.RecordRequest(
                    context.Request.Method,
                    context.Request.Path,
                    context.Response.StatusCode,
                    stopwatch.ElapsedMilliseconds);
            }
        }
    }
}