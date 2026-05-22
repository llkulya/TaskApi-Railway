using Castle.DynamicProxy;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using TaskApi.Attributes;

namespace TaskApi.Interceptors
{
    public class CachingInterceptor : IInterceptor
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<CachingInterceptor> _logger;

        // Зберігаємо ключі, щоб можна було очищати кеш після Create/Update/Delete
        private static readonly ConcurrentDictionary<string, byte> _cacheKeys = new();

        public CachingInterceptor(
            IMemoryCache cache,
            ILogger<CachingInterceptor> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public void Intercept(IInvocation invocation)
        {
            var method = invocation.MethodInvocationTarget ?? invocation.Method;
            var targetType = invocation.InvocationTarget?.GetType() ?? method.DeclaringType;

            var className = targetType?.Name ?? "UnknownClass";
            var methodName = method.Name;

            // [NoIntercept] має повністю вимикати інтерцепцію
            if (method.GetCustomAttribute<NoInterceptAttribute>() != null ||
                targetType?.GetCustomAttribute<NoInterceptAttribute>() != null)
            {
                invocation.Proceed();
                return;
            }

            // Якщо це метод зміни даних — виконуємо його і після успіху чистимо кеш
            if (IsMutationMethod(methodName))
            {
                HandleMutationMethod(invocation, className, methodName);
                return;
            }

            var cacheAttribute =
                method.GetCustomAttribute<CacheAttribute>() ??
                invocation.Method.GetCustomAttribute<CacheAttribute>();

            // Якщо [Cache] немає — просто виконуємо метод
            if (cacheAttribute == null)
            {
                invocation.Proceed();
                return;
            }

            var returnType = invocation.Method.ReturnType;

            if (returnType == typeof(Task))
            {
                // Task без результату кешувати немає сенсу
                invocation.Proceed();
                return;
            }

            if (returnType.IsGenericType &&
                returnType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                var resultType = returnType.GetGenericArguments()[0];

                var methodInfo = typeof(CachingInterceptor)
                    .GetMethod(
                        nameof(HandleTaskWithResultAsync),
                        BindingFlags.NonPublic | BindingFlags.Instance)!
                    .MakeGenericMethod(resultType);

                invocation.ReturnValue = methodInfo.Invoke(
                    this,
                    new object[]
                    {
                        invocation,
                        cacheAttribute,
                        className,
                        methodName
                    });

                return;
            }

            // Синхронний метод з результатом
            var cacheKey = GenerateCacheKey(className, methodName, invocation.Arguments);

            if (_cache.TryGetValue(cacheKey, out var cachedValue))
            {
                _logger.LogInformation(
                    "AOP Cache: HIT для методу {ClassName}.{MethodName}. Key: {CacheKey}",
                    className,
                    methodName,
                    cacheKey);

                invocation.ReturnValue = cachedValue;
                return;
            }

            invocation.Proceed();

            _cache.Set(
                cacheKey,
                invocation.ReturnValue,
                TimeSpan.FromSeconds(cacheAttribute.DurationSeconds));

            _cacheKeys.TryAdd(cacheKey, 0);

            _logger.LogInformation(
                "AOP Cache: MISS для методу {ClassName}.{MethodName}. Результат збережено в кеш на {DurationSeconds} сек. Key: {CacheKey}",
                className,
                methodName,
                cacheAttribute.DurationSeconds,
                cacheKey);
        }

        private async Task<T> HandleTaskWithResultAsync<T>(
            IInvocation invocation,
            CacheAttribute cacheAttribute,
            string className,
            string methodName)
        {
            var cacheKey = GenerateCacheKey(className, methodName, invocation.Arguments);

            if (_cache.TryGetValue(cacheKey, out T? cachedValue))
            {
                _logger.LogInformation(
                    "AOP Cache: HIT для методу {ClassName}.{MethodName}. Key: {CacheKey}",
                    className,
                    methodName,
                    cacheKey);

                return cachedValue!;
            }

            invocation.Proceed();

            var task = (Task<T>)invocation.ReturnValue!;
            var result = await task;

            _cache.Set(
                cacheKey,
                result,
                TimeSpan.FromSeconds(cacheAttribute.DurationSeconds));

            _cacheKeys.TryAdd(cacheKey, 0);

            _logger.LogInformation(
                "AOP Cache: MISS для методу {ClassName}.{MethodName}. Результат збережено в кеш на {DurationSeconds} сек. Key: {CacheKey}",
                className,
                methodName,
                cacheAttribute.DurationSeconds,
                cacheKey);

            return result;
        }

        private void HandleMutationMethod(
            IInvocation invocation,
            string className,
            string methodName)
        {
            var returnType = invocation.Method.ReturnType;

            if (returnType == typeof(Task))
            {
                invocation.Proceed();

                var task = (Task)invocation.ReturnValue!;
                invocation.ReturnValue = ClearCacheAfterTaskAsync(
                    task,
                    className,
                    methodName);

                return;
            }

            if (returnType.IsGenericType &&
                returnType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                invocation.Proceed();

                var resultType = returnType.GetGenericArguments()[0];

                var methodInfo = typeof(CachingInterceptor)
                    .GetMethod(
                        nameof(ClearCacheAfterTaskWithResultAsync),
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

                return;
            }

            invocation.Proceed();
            ClearAllTrackedCache(className, methodName);
        }

        private async Task ClearCacheAfterTaskAsync(
            Task task,
            string className,
            string methodName)
        {
            await task;
            ClearAllTrackedCache(className, methodName);
        }

        private async Task<T> ClearCacheAfterTaskWithResultAsync<T>(
            Task<T> task,
            string className,
            string methodName)
        {
            var result = await task;
            ClearAllTrackedCache(className, methodName);
            return result;
        }

        private void ClearAllTrackedCache(string className, string methodName)
        {
            foreach (var key in _cacheKeys.Keys)
            {
                _cache.Remove(key);
                _cacheKeys.TryRemove(key, out _);
            }

            _logger.LogInformation(
                "AOP Cache: кеш очищено після виконання методу {ClassName}.{MethodName}",
                className,
                methodName);
        }

        private static bool IsMutationMethod(string methodName)
        {
            return methodName.StartsWith("Create", StringComparison.OrdinalIgnoreCase)
                || methodName.StartsWith("Update", StringComparison.OrdinalIgnoreCase)
                || methodName.StartsWith("Delete", StringComparison.OrdinalIgnoreCase)
                || methodName.StartsWith("Bulk", StringComparison.OrdinalIgnoreCase)
                || methodName.StartsWith("Assign", StringComparison.OrdinalIgnoreCase)
                || methodName.StartsWith("Change", StringComparison.OrdinalIgnoreCase);
        }

        private static string GenerateCacheKey(
            string className,
            string methodName,
            object?[] arguments)
        {
            string serializedArguments;

            try
            {
                serializedArguments = JsonSerializer.Serialize(arguments);
            }
            catch
            {
                serializedArguments = string.Join("_",
                    arguments.Select(a => a?.GetHashCode().ToString() ?? "null"));
            }

            return $"AOP_CACHE:{className}:{methodName}:{serializedArguments}";
        }
    }
}