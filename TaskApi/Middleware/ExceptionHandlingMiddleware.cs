using System.Net;
using System.Text.Json;
using TaskApi.Exceptions;

namespace TaskApi.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "An unhandled exception: {Message}", exception.Message);

                // ⚠️ Не перехоплюємо помилки OAuth callback
                if (context.Request.Path.StartsWithSegments("/api/sso"))
                {
                    context.Response.ContentType = "application/json";
                    context.Response.StatusCode = 500;
                    await context.Response.WriteAsync(JsonSerializer.Serialize(new
                    {
                        message = exception.Message,
                        detail = exception.InnerException?.Message,
                        type = "SsoError"
                    }));
                    return;
                }

                await HandleExceptionAsync(context, exception);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            int statusCode;
            string type;

            switch (exception)
            {
                case KeyNotFoundException:
                    statusCode = (int)HttpStatusCode.NotFound;
                    type = "NotFound";
                    break;

                case UnauthorizedException:
                    statusCode = (int)HttpStatusCode.Unauthorized;
                    type = "UnauthorizedError";
                    break;

                case InvalidOperationException:
                case System.ComponentModel.DataAnnotations.ValidationException:
                    statusCode = (int)HttpStatusCode.BadRequest;
                    type = "ValidationError";
                    break;

                case ConcurrencyException:
                    statusCode = (int)HttpStatusCode.Conflict;
                    type = "ConcurrencyError";
                    break;

                default:
                    statusCode = (int)HttpStatusCode.InternalServerError;
                    type = "ServerError";
                    break;
            }

            var result = new
            {
                message = exception.Message,
                type = type
            };

            context.Response.StatusCode = statusCode;
            return context.Response.WriteAsync(JsonSerializer.Serialize(result));
        }
    }
}