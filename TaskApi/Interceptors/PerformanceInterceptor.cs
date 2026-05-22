using Castle.DynamicProxy;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Reflection;
using TaskApi.Attributes;

namespace TaskApi.Interceptors
{
    public class PerformanceInterceptor : IInterceptor
    {
        private readonly ILogger<PerformanceInterceptor> _logger;

        public PerformanceInterceptor(
            ILogger<PerformanceInterceptor> logger)
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

            // Шукаємо [MeasureTime] на методі або класі
            var measureAttribute =
                method.GetCustomAttribute<MeasureTimeAttribute>() ??
                targetType?.GetCustomAttribute<MeasureTimeAttribute>();

            // Якщо [MeasureTime] немає — просто виконуємо метод
            if (measureAttribute == null)
            {
                invocation.Proceed();
                return;
            }

            var stopwatch = Stopwatch.StartNew();

            try
            {
                invocation.Proceed();

                var returnType = invocation.Method.ReturnType;

                if (returnType == typeof(Task))
                {
                    var task = (Task)invocation.ReturnValue!;
                    invocation.ReturnValue = MeasureTaskAsync(
                        task,
                        stopwatch,
                        className,
                        methodName,
                        measureAttribute);
                }
                else if (returnType.IsGenericType &&
                         returnType.GetGenericTypeDefinition() == typeof(Task<>))
                {
                    var resultType = returnType.GetGenericArguments()[0];

                    var methodInfo = typeof(PerformanceInterceptor)
                        .GetMethod(
                            nameof(MeasureTaskWithResultAsync),
                            BindingFlags.NonPublic | BindingFlags.Instance)!
                        .MakeGenericMethod(resultType);

                    invocation.ReturnValue = methodInfo.Invoke(
                        this,
                        new object[]
                        {
                            invocation.ReturnValue!,
                            stopwatch,
                            className,
                            methodName,
                            measureAttribute
                        });
                }
                else
                {
                    stopwatch.Stop();

                    LogPerformance(
                        stopwatch.ElapsedMilliseconds,
                        className,
                        methodName,
                        measureAttribute);
                }
            }
            catch
            {
                stopwatch.Stop();

                _logger.LogError(
                    "AOP Performance: Метод {ClassName}.{MethodName} завершився з помилкою за {ElapsedMs} мс",
                    className,
                    methodName,
                    stopwatch.ElapsedMilliseconds);

                throw;
            }
        }

        private async Task MeasureTaskAsync(
            Task task,
            Stopwatch stopwatch,
            string className,
            string methodName,
            MeasureTimeAttribute attribute)
        {
            try
            {
                await task;
            }
            finally
            {
                stopwatch.Stop();

                LogPerformance(
                    stopwatch.ElapsedMilliseconds,
                    className,
                    methodName,
                    attribute);
            }
        }

        private async Task<T> MeasureTaskWithResultAsync<T>(
            Task<T> task,
            Stopwatch stopwatch,
            string className,
            string methodName,
            MeasureTimeAttribute attribute)
        {
            try
            {
                return await task;
            }
            finally
            {
                stopwatch.Stop();

                LogPerformance(
                    stopwatch.ElapsedMilliseconds,
                    className,
                    methodName,
                    attribute);
            }
        }

        private void LogPerformance(
            long elapsedMs,
            string className,
            string methodName,
            MeasureTimeAttribute attribute)
        {
            if (elapsedMs > attribute.ErrorThresholdMs)
            {
                _logger.LogError(
                    "AOP Performance: Метод {ClassName}.{MethodName} виконувався занадто довго: {ElapsedMs} мс. Поріг Error: {ErrorThresholdMs} мс",
                    className,
                    methodName,
                    elapsedMs,
                    attribute.ErrorThresholdMs);
            }
            else if (elapsedMs > attribute.WarningThresholdMs)
            {
                _logger.LogWarning(
                    "AOP Performance: Метод {ClassName}.{MethodName} виконувався довше норми: {ElapsedMs} мс. Поріг Warning: {WarningThresholdMs} мс",
                    className,
                    methodName,
                    elapsedMs,
                    attribute.WarningThresholdMs);
            }
            else
            {
                _logger.LogInformation(
                    "AOP Performance: Метод {ClassName}.{MethodName} виконався за {ElapsedMs} мс",
                    className,
                    methodName,
                    elapsedMs);
            }
        }
    }
}