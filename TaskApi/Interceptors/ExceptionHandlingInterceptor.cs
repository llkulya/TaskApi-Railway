using Castle.DynamicProxy;
using Microsoft.Extensions.Logging;
using System.Reflection;
using TaskApi.Attributes;

namespace TaskApi.Interceptors
{
    public class ExceptionHandlingInterceptor : IInterceptor
    {
        private readonly ILogger<ExceptionHandlingInterceptor> _logger;

        public ExceptionHandlingInterceptor(
            ILogger<ExceptionHandlingInterceptor> logger)
        {
            _logger = logger;
        }

        public void Intercept(IInvocation invocation)
        {
            var method = invocation.MethodInvocationTarget ?? invocation.Method;
            var targetType = invocation.InvocationTarget?.GetType() ?? method.DeclaringType;

            var className = targetType?.Name ?? "UnknownClass";
            var methodName = method.Name;

            // Якщо метод або клас позначений [NoIntercept] — пропускаємо
            if (method.GetCustomAttribute<NoInterceptAttribute>() != null ||
                targetType?.GetCustomAttribute<NoInterceptAttribute>() != null)
            {
                invocation.Proceed();
                return;
            }

            try
            {
                invocation.Proceed();

                var returnType = invocation.Method.ReturnType;

                if (returnType == typeof(Task))
                {
                    var task = (Task)invocation.ReturnValue!;
                    invocation.ReturnValue = HandleTaskAsync(task, className, methodName);
                }
                else if (returnType.IsGenericType &&
                         returnType.GetGenericTypeDefinition() == typeof(Task<>))
                {
                    var resultType = returnType.GetGenericArguments()[0];

                    var methodInfo = typeof(ExceptionHandlingInterceptor)
                        .GetMethod(
                            nameof(HandleTaskWithResultAsync),
                            BindingFlags.NonPublic | BindingFlags.Instance)!
                        .MakeGenericMethod(resultType);

                    invocation.ReturnValue = methodInfo.Invoke(
                        this,
                        new object[]
                        {
                            invocation.ReturnValue!,
                            className,
                            methodName
                        });
                }
            }
            catch (Exception ex)
            {
                LogException(ex, className, methodName);
                throw;
            }
        }

        private async Task HandleTaskAsync(
            Task task,
            string className,
            string methodName)
        {
            try
            {
                await task;
            }
            catch (Exception ex)
            {
                LogException(ex, className, methodName);
                throw;
            }
        }

        private async Task<T> HandleTaskWithResultAsync<T>(
            Task<T> task,
            string className,
            string methodName)
        {
            try
            {
                return await task;
            }
            catch (Exception ex)
            {
                LogException(ex, className, methodName);
                throw;
            }
        }

        private void LogException(
            Exception ex,
            string className,
            string methodName)
        {
            _logger.LogError(
                ex,
                "AOP Exception: помилка у методі {ClassName}.{MethodName}. Type: {ExceptionType}. Message: {ErrorMessage}",
                className,
                methodName,
                ex.GetType().Name,
                ex.Message);
        }
    }
}