using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System.ComponentModel.DataAnnotations;
using TaskApi.Dto.Commands;
using TaskApi.Interceptors;

namespace TaskApi.Tests
{
    [TestFixture]
    public class ValidationInterceptorTests
    {
        private Mock<ILogger<ValidationInterceptor>> _mockLogger = null!;
        private ValidationInterceptor _interceptor = null!;

        [SetUp]
        public void Setup()
        {
            _mockLogger = new Mock<ILogger<ValidationInterceptor>>();
            _interceptor = new ValidationInterceptor(_mockLogger.Object);
        }

        [Test]
        public void Intercept_ValidDto_ShouldProceed()
        {
            // Arrange
            var command = new TaskItemCreateCommand
            {
                Title = "Valid task",
                Description = "Valid description",
                DueDate = DateTime.UtcNow.AddDays(1),
                Status = "Pending",
                Priority = "Medium",
                ProjectId = 1,
                ExecutorId = 1
            };

            var methodInfo = typeof(FakeService)
                .GetMethod(nameof(FakeService.Create))!;

            var invocation = new Mock<Castle.DynamicProxy.IInvocation>();

            invocation.Setup(i => i.Method)
                .Returns(methodInfo);

            invocation.Setup(i => i.MethodInvocationTarget)
                .Returns(methodInfo);

            invocation.Setup(i => i.InvocationTarget)
                .Returns(new FakeService());

            invocation.Setup(i => i.Arguments)
                .Returns(new object[] { command });

            // Act
            _interceptor.Intercept(invocation.Object);

            // Assert
            invocation.Verify(i => i.Proceed(), Times.Once);
        }

        [Test]
        public void Intercept_InvalidDto_ShouldThrowValidationException()
        {
            // Arrange
            var command = new TaskItemCreateCommand
            {
                Title = "",
                Description = "Test",
                DueDate = DateTime.UtcNow.AddDays(1),
                Status = "Pending",
                Priority = "Medium",
                ProjectId = 1,
                ExecutorId = 1
            };

            var methodInfo = typeof(FakeService)
                .GetMethod(nameof(FakeService.Create))!;

            var invocation = new Mock<Castle.DynamicProxy.IInvocation>();

            invocation.Setup(i => i.Method)
                .Returns(methodInfo);

            invocation.Setup(i => i.MethodInvocationTarget)
                .Returns(methodInfo);

            invocation.Setup(i => i.InvocationTarget)
                .Returns(new FakeService());

            invocation.Setup(i => i.Arguments)
                .Returns(new object[] { command });

            // Act & Assert
            Assert.Throws<ValidationException>(() =>
                _interceptor.Intercept(invocation.Object));

            invocation.Verify(i => i.Proceed(), Times.Never);
        }

        private class FakeService
        {
            public void Create(TaskItemCreateCommand command)
            {
            }
        }
    }
}