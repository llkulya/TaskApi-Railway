using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using TaskApi.Attributes;
using TaskApi.Interceptors;

namespace TaskApi.Tests
{
    [TestFixture]
    public class MethodLoggingInterceptorTests
    {
        private Mock<ILogger<MethodLoggingInterceptor>> _mockLogger = null!;
        private MethodLoggingInterceptor _interceptor = null!;

        [SetUp]
        public void Setup()
        {
            _mockLogger = new Mock<ILogger<MethodLoggingInterceptor>>();
            _interceptor = new MethodLoggingInterceptor(_mockLogger.Object);
        }

        [Test]
        public void Intercept_MethodWithLogMethodAttribute_ShouldProceedAndLog()
        {
            // Arrange
            var methodInfo = typeof(FakeLoggedService)
                .GetMethod(nameof(FakeLoggedService.DoWork))!;

            var invocation = new Mock<Castle.DynamicProxy.IInvocation>();

            invocation.Setup(i => i.Method).Returns(methodInfo);
            invocation.Setup(i => i.MethodInvocationTarget).Returns(methodInfo);
            invocation.Setup(i => i.InvocationTarget).Returns(new FakeLoggedService());
            invocation.Setup(i => i.Arguments).Returns(Array.Empty<object>());

            // Act
            _interceptor.Intercept(invocation.Object);

            // Assert
            invocation.Verify(i => i.Proceed(), Times.Once);

            VerifyLog(
                LogLevel.Information,
                "AOP: Виклик методу",
                Times.Once());
        }

        [Test]
        public void Intercept_MethodWithNoInterceptAttribute_ShouldProceedWithoutLogging()
        {
            // Arrange
            var methodInfo = typeof(FakeLoggedService)
                .GetMethod(nameof(FakeLoggedService.DoWithoutIntercept))!;

            var invocation = new Mock<Castle.DynamicProxy.IInvocation>();

            invocation.Setup(i => i.Method).Returns(methodInfo);
            invocation.Setup(i => i.MethodInvocationTarget).Returns(methodInfo);
            invocation.Setup(i => i.InvocationTarget).Returns(new FakeLoggedService());
            invocation.Setup(i => i.Arguments).Returns(Array.Empty<object>());

            // Act
            _interceptor.Intercept(invocation.Object);

            // Assert
            invocation.Verify(i => i.Proceed(), Times.Once);

            VerifyLog(
                LogLevel.Information,
                "AOP: Виклик методу",
                Times.Never());
        }

        private void VerifyLog(LogLevel level, string containsText, Times times)
        {
            _mockLogger.Verify(
                x => x.Log(
                    level,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) =>
                        v.ToString() != null &&
                        v.ToString()!.Contains(containsText)),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                times);
        }

        [LogMethod(Level = LogLevel.Information)]
        private class FakeLoggedService
        {
            public void DoWork()
            {
            }

            [NoIntercept]
            public void DoWithoutIntercept()
            {
            }
        }
    }
}