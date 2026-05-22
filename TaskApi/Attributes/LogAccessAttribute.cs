using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace TaskApi.Attributes
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class LogAccessAttribute : Attribute, IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(
            ActionExecutingContext context,
            ActionExecutionDelegate next)
        {
            var loggerFactory = context.HttpContext.RequestServices
                .GetRequiredService<ILoggerFactory>();

            var logger = loggerFactory.CreateLogger<LogAccessAttribute>();

            var userId =
                context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? context.HttpContext.User.FindFirst("sub")?.Value
                ?? "Anonymous";

            var method = context.HttpContext.Request.Method;
            var path = context.HttpContext.Request.Path;
            var ipAddress = context.HttpContext.Connection.RemoteIpAddress?.ToString();

            logger.LogInformation(
                "AOP Access: користувач {UserId} звернувся до {Method} {Path} з IP {IpAddress}",
                userId,
                method,
                path,
                ipAddress);

            await next();
        }
    }
}