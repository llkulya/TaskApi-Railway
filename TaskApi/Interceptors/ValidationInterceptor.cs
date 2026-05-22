using Castle.DynamicProxy;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using TaskApi.Attributes;

namespace TaskApi.Interceptors
{
    public class ValidationInterceptor : IInterceptor
    {
        private readonly ILogger<ValidationInterceptor> _logger;

        public ValidationInterceptor(ILogger<ValidationInterceptor> logger)
        {
            _logger = logger;
        }

        public void Intercept(IInvocation invocation)
        {
            var method = invocation.MethodInvocationTarget ?? invocation.Method;
            var targetType = invocation.InvocationTarget?.GetType() ?? method.DeclaringType;

            var className = targetType?.Name ?? "UnknownClass";
            var methodName = method.Name;

            // Якщо метод або клас позначений [NoIntercept] — пропускаємо всю інтерцепцію
            if (method.GetCustomAttribute<NoInterceptAttribute>() != null ||
                targetType?.GetCustomAttribute<NoInterceptAttribute>() != null)
            {
                invocation.Proceed();
                return;
            }

            var parameters = method.GetParameters();

            for (int i = 0; i < invocation.Arguments.Length; i++)
            {
                var argument = invocation.Arguments[i];
                var parameter = parameters[i];

                ValidateNull(argument, parameter, methodName);
                ValidateDataAnnotations(argument, parameter.Name ?? $"arg{i}", methodName);
            }

            _logger.LogInformation(
                "AOP Validation: метод {ClassName}.{MethodName} пройшов валідацію",
                className,
                methodName);

            invocation.Proceed();
        }

        private void ValidateNull(object? argument, ParameterInfo parameter, string methodName)
        {
            if (argument != null)
                return;

            var hasRequiredAttribute =
                parameter.GetCustomAttribute<RequiredAttribute>() != null;

            var isNullableValueType =
                Nullable.GetUnderlyingType(parameter.ParameterType) != null;

            var isValueType = parameter.ParameterType.IsValueType;

            var hasDefaultValue = parameter.HasDefaultValue;

            if (hasRequiredAttribute || (isValueType && !isNullableValueType && !hasDefaultValue))
            {
                _logger.LogWarning(
                    "AOP Validation: параметр {ParameterName} у методі {MethodName} є null",
                    parameter.Name,
                    methodName);

                throw new ArgumentNullException(
                    parameter.Name,
                    $"Параметр '{parameter.Name}' у методі {methodName} не може бути null.");
            }
        }

        private void ValidateDataAnnotations(object? argument, string parameterName, string methodName)
        {
            if (argument == null)
                return;

            var type = argument.GetType();

            if (type.IsPrimitive ||
                type == typeof(string) ||
                type == typeof(DateTime) ||
                type == typeof(decimal) ||
                type.IsEnum)
            {
                return;
            }

            var properties = type.GetProperties();

            var hasValidationAttributes = properties.Any(p =>
                p.GetCustomAttributes(typeof(ValidationAttribute), true).Any());

            if (!hasValidationAttributes)
                return;

            var validationContext = new ValidationContext(argument);
            var validationResults = new List<ValidationResult>();

            var isValid = Validator.TryValidateObject(
                argument,
                validationContext,
                validationResults,
                validateAllProperties: true);

            if (!isValid)
            {
                var errors = string.Join("; ",
                    validationResults.Select(r => r.ErrorMessage));

                _logger.LogWarning(
                    "AOP Validation: параметр {ParameterName} у методі {MethodName} не пройшов валідацію: {Errors}",
                    parameterName,
                    methodName,
                    errors);

                throw new ValidationException(
                    $"Валідація параметра '{parameterName}' у методі {methodName} не пройшла: {errors}");
            }
        }
    }
}