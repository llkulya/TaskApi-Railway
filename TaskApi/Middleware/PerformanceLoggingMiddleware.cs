using System.Diagnostics;
using Serilog.Context;

namespace TaskApi.Middleware
{
    public class PerformanceLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<PerformanceLoggingMiddleware> _logger;

        public PerformanceLoggingMiddleware(
            RequestDelegate next,
            ILogger<PerformanceLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();

            await _next(context);

            stopwatch.Stop();

            var elapsedMs = stopwatch.ElapsedMilliseconds;

            // Додаємо в лог контекст
            using (LogContext.PushProperty("ElapsedMs", elapsedMs))
            {
                if (elapsedMs > 5000)
                {
                    _logger.LogError(
                        "Request {Method} {Path} took {ElapsedMs} ms - CRITICAL",
                        context.Request.Method,
                        context.Request.Path,
                        elapsedMs);
                }
                else if (elapsedMs > 2000)
                {
                    _logger.LogWarning(
                        "Request {Method} {Path} took {ElapsedMs} ms - SLOW",
                        context.Request.Method,
                        context.Request.Path,
                        elapsedMs);
                }
                else
                {
                    _logger.LogInformation(
                        "Request {Method} {Path} took {ElapsedMs} ms",
                        context.Request.Method,
                        context.Request.Path,
                        elapsedMs);
                }
            }
        }
    }
}