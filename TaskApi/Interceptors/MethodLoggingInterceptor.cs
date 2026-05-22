using Castle.DynamicProxy;
using Microsoft.Extensions.Logging;
using System.Reflection;
using TaskApi.Attributes;

namespace TaskApi.Interceptors
{
    public class MethodLoggingInterceptor : IInterceptor
    {
        private readonly ILogger<MethodLoggingInterceptor> _logger;

        public MethodLoggingInterceptor(
            ILogger<MethodLoggingInterceptor> logger)
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

            // Перевіряємо [LogMethod] на методі або класі
            var logAttribute =
                method.GetCustomAttribute<LogMethodAttribute>() ??
                targetType?.GetCustomAttribute<LogMethodAttribute>();

            // Якщо [LogMethod] немає — просто виконуємо метод
            if (logAttribute == null)
            {
                invocation.Proceed();
                return;
            }

            _logger.Log(
                logAttribute.Level,
                "AOP: Виклик методу {ClassName}.{MethodName} з параметрами {@Parameters}",
                className,
                methodName,
                invocation.Arguments);

            try
            {
                invocation.Proceed();

                var returnType = invocation.Method.ReturnType;

                if (returnType == typeof(Task))
                {
                    var task = (Task)invocation.ReturnValue!;
                    invocation.ReturnValue = HandleTaskAsync(
                        task,
                        className,
                        methodName,
                        logAttribute.Level);
                }
                else if (returnType.IsGenericType &&
                         returnType.GetGenericTypeDefinition() == typeof(Task<>))
                {
                    var resultType = returnType.GetGenericArguments()[0];

                    var methodInfo = typeof(MethodLoggingInterceptor)
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
                            methodName,
                            logAttribute.Level
                        });
                }
                else
                {
                    _logger.Log(
                        logAttribute.Level,
                        "AOP: Метод {ClassName}.{MethodName} повернув результат {@Result}",
                        className,
                        methodName,
                        invocation.ReturnValue);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "AOP: Помилка при виконанні методу {ClassName}.{MethodName}: {ErrorMessage}",
                    className,
                    methodName,
                    ex.Message);

                throw;
            }
        }

        private async Task HandleTaskAsync(
            Task task,
            string className,
            string methodName,
            LogLevel level)
        {
            try
            {
                await task;

                _logger.Log(
                    level,
                    "AOP: Метод {ClassName}.{MethodName} завершився успішно",
                    className,
                    methodName);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "AOP: Помилка в асинхронному методі {ClassName}.{MethodName}: {ErrorMessage}",
                    className,
                    methodName,
                    ex.Message);

                throw;
            }
        }

        private async Task<T> HandleTaskWithResultAsync<T>(
            Task<T> task,
            string className,
            string methodName,
            LogLevel level)
        {
            try
            {
                var result = await task;

                _logger.Log(
                    level,
                    "AOP: Метод {ClassName}.{MethodName} повернув результат {@Result}",
                    className,
                    methodName,
                    result);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "AOP: Помилка в асинхронному методі {ClassName}.{MethodName}: {ErrorMessage}",
                    className,
                    methodName,
                    ex.Message);

                throw;
            }
        }
    }
}