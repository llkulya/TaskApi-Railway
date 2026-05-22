using Castle.DynamicProxy;
using HRService.Attributes;
using System.Net.Http;
using System.Reflection;

namespace HRService.Interceptors
{
    public class RetryInterceptor : IInterceptor
    {
        private readonly ILogger<RetryInterceptor> _logger;

        public RetryInterceptor(ILogger<RetryInterceptor> logger)
        {
            _logger = logger;
        }

        public void Intercept(IInvocation invocation)
        {
            var retryAttribute = GetRetryAttribute(invocation);

            if (retryAttribute == null)
            {
                invocation.Proceed();
                return;
            }

            var returnType = invocation.Method.ReturnType;

            if (returnType == typeof(Task))
            {
                invocation.ReturnValue = InterceptAsync(invocation, retryAttribute);
                return;
            }

            if (returnType.IsGenericType &&
                returnType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                var resultType = returnType.GetGenericArguments()[0];

                var method = typeof(RetryInterceptor)
                    .GetMethod(
                        nameof(InterceptAsyncWithResult),
                        BindingFlags.Instance | BindingFlags.NonPublic)!
                    .MakeGenericMethod(resultType);

                invocation.ReturnValue = method.Invoke(
                    this,
                    new object[] { invocation, retryAttribute });

                return;
            }

            invocation.Proceed();
        }

        private async Task InterceptAsync(
            IInvocation invocation,
            RetryAttribute retryAttribute)
        {
            var proceed = invocation.CaptureProceedInfo();
            var employeeEmail = GetEmployeeEmail(invocation.Arguments);

            for (int attempt = 1; attempt <= retryAttribute.MaxAttempts; attempt++)
            {
                try
                {
                    LogAttempt(invocation, attempt, employeeEmail);

                    proceed.Invoke();

                    var task = (Task)invocation.ReturnValue!;
                    await task;

                    LogSuccess(invocation, attempt, employeeEmail);
                    return;
                }
                catch (Exception ex) when (ShouldRetry(ex) &&
                                           attempt < retryAttribute.MaxAttempts)
                {
                    LogRetry(invocation, attempt, employeeEmail, ex);

                    await Task.Delay(retryAttribute.DelayMilliseconds);
                }
                catch (Exception ex)
                {
                    LogFailed(invocation, attempt, employeeEmail, ex);
                    throw;
                }
            }
        }

        private async Task<T> InterceptAsyncWithResult<T>(
            IInvocation invocation,
            RetryAttribute retryAttribute)
        {
            var proceed = invocation.CaptureProceedInfo();
            var employeeEmail = GetEmployeeEmail(invocation.Arguments);

            for (int attempt = 1; attempt <= retryAttribute.MaxAttempts; attempt++)
            {
                try
                {
                    LogAttempt(invocation, attempt, employeeEmail);

                    proceed.Invoke();

                    var task = (Task<T>)invocation.ReturnValue!;
                    var result = await task;

                    LogSuccess(invocation, attempt, employeeEmail);
                    return result;
                }
                catch (Exception ex) when (ShouldRetry(ex) &&
                                           attempt < retryAttribute.MaxAttempts)
                {
                    LogRetry(invocation, attempt, employeeEmail, ex);

                    await Task.Delay(retryAttribute.DelayMilliseconds);
                }
                catch (Exception ex)
                {
                    LogFailed(invocation, attempt, employeeEmail, ex);
                    throw;
                }
            }

            throw new InvalidOperationException("Retry interceptor finished unexpectedly.");
        }

        private static RetryAttribute? GetRetryAttribute(IInvocation invocation)
        {
            return invocation.MethodInvocationTarget
                       .GetCustomAttribute<RetryAttribute>()
                   ?? invocation.Method
                       .GetCustomAttribute<RetryAttribute>();
        }

        private static bool ShouldRetry(Exception ex)
        {
            return ex is HttpRequestException ||
                   ex is TaskCanceledException;
        }

        private static string GetEmployeeEmail(object[] arguments)
        {
            foreach (var argument in arguments)
            {
                if (argument == null)
                    continue;

                var property = argument.GetType().GetProperty("Email");

                if (property?.GetValue(argument) is string email &&
                    !string.IsNullOrWhiteSpace(email))
                {
                    return email;
                }
            }

            return "unknown";
        }

        private void LogAttempt(
            IInvocation invocation,
            int attempt,
            string employeeEmail)
        {
            _logger.LogInformation(
                "Retry attempt started. ServiceName={ServiceName}, MethodName={MethodName}, AttemptNumber={AttemptNumber}, Success={Success}, EmployeeEmail={EmployeeEmail}",
                "HRService",
                invocation.Method.Name,
                attempt,
                false,
                employeeEmail);
        }

        private void LogSuccess(
            IInvocation invocation,
            int attempt,
            string employeeEmail)
        {
            _logger.LogInformation(
                "Retry attempt succeeded. ServiceName={ServiceName}, MethodName={MethodName}, AttemptNumber={AttemptNumber}, Success={Success}, EmployeeEmail={EmployeeEmail}",
                "HRService",
                invocation.Method.Name,
                attempt,
                true,
                employeeEmail);
        }

        private void LogRetry(
            IInvocation invocation,
            int attempt,
            string employeeEmail,
            Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Retry attempt failed, will retry. ServiceName={ServiceName}, MethodName={MethodName}, AttemptNumber={AttemptNumber}, Success={Success}, EmployeeEmail={EmployeeEmail}",
                "HRService",
                invocation.Method.Name,
                attempt,
                false,
                employeeEmail);
        }

        private void LogFailed(
            IInvocation invocation,
            int attempt,
            string employeeEmail,
            Exception ex)
        {
            _logger.LogError(
                ex,
                "All retry attempts failed. ServiceName={ServiceName}, MethodName={MethodName}, AttemptNumber={AttemptNumber}, Success={Success}, EmployeeEmail={EmployeeEmail}",
                "HRService",
                invocation.Method.Name,
                attempt,
                false,
                employeeEmail);
        }
    }
}