using Microsoft.Extensions.Logging;

namespace TaskApi.Attributes
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class LogMethodAttribute : Attribute
    {
        public LogLevel Level { get; set; } = LogLevel.Information;
    }
}