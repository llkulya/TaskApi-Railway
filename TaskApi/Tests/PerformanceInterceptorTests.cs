using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using TaskApi.Attributes;
using TaskApi.Interceptors;

namespace TaskApi.Tests
{
    [TestFixture]
    public class PerformanceInterceptorTests
    {
        private Mock<ILogger<PerformanceInterceptor>> _mockLogger = null!;
        private PerformanceInterceptor _interceptor = null!;

        [SetUp]
        public void Setup()
        {
            _mockLogger = new Mock<ILogger<PerformanceInterceptor>>();
            _interceptor = new PerformanceInterceptor(_mockLogger.Object);
        }

        [Test]
        public void Intercept_MethodWithMeasureTimeAttribute_ShouldProceedAndLogPerformance()
        {
            // Arrange
            var methodInfo = typeof(FakePerformanceService)
                .GetMethod(nameof(FakePerformanceService.FastWork))!;

            var invocation = new Mock<Castle.DynamicProxy.IInvocation>();

            invocation.Setup(i => i.Method).Returns(methodInfo);
            invocation.Setup(i => i.MethodInvocationTarget).Returns(methodInfo);
            invocation.Setup(i => i.InvocationTarget).Returns(new FakePerformanceService());
            invocation.Setup(i => i.Arguments).Returns(Array.Empty<object>());

            // Act
            _interceptor.Intercept(invocation.Object);

            // Assert
            invocation.Verify(i => i.Proceed(), Times.Once);

            VerifyLog(
                LogLevel.Information,
                "AOP Performance",
                Times.Once());
        }

        [Test]
        public void Intercept_SlowMethod_ShouldLogWarning()
        {
            // Arrange
            var methodInfo = typeof(FakePerformanceService)
                .GetMethod(nameof(FakePerformanceService.SlowWork))!;

            var invocation = new Mock<Castle.DynamicProxy.IInvocation>();

            invocation.Setup(i => i.Method).Returns(methodInfo);
            invocation.Setup(i => i.MethodInvocationTarget).Returns(methodInfo);
            invocation.Setup(i => i.InvocationTarget).Returns(new FakePerformanceService());
            invocation.Setup(i => i.Arguments).Returns(Array.Empty<object>());

            invocation.Setup(i => i.Proceed())
                .Callback(() => Thread.Sleep(30));

            // Act
            _interceptor.Intercept(invocation.Object);

            // Assert
            invocation.Verify(i => i.Proceed(), Times.Once);

            VerifyLog(
                LogLevel.Warning,
                "AOP Performance",
                Times.Once());
        }

        [Test]
        public void Intercept_MethodWithNoInterceptAttribute_ShouldProceedWithoutPerformanceLog()
        {
            // Arrange
            var methodInfo = typeof(FakePerformanceService)
                .GetMethod(nameof(FakePerformanceService.WorkWithoutIntercept))!;

            var invocation = new Mock<Castle.DynamicProxy.IInvocation>();

            invocation.Setup(i => i.Method).Returns(methodInfo);
            invocation.Setup(i => i.MethodInvocationTarget).Returns(methodInfo);
            invocation.Setup(i => i.InvocationTarget).Returns(new FakePerformanceService());
            invocation.Setup(i => i.Arguments).Returns(Array.Empty<object>());

            // Act
            _interceptor.Intercept(invocation.Object);

            // Assert
            invocation.Verify(i => i.Proceed(), Times.Once);

            VerifyLog(
                LogLevel.Information,
                "AOP Performance",
                Times.Never());

            VerifyLog(
                LogLevel.Warning,
                "AOP Performance",
                Times.Never());

            VerifyLog(
                LogLevel.Error,
                "AOP Performance",
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

        private class FakePerformanceService
        {
            [MeasureTime(WarningThresholdMs = 1000, ErrorThresholdMs = 5000)]
            public void FastWork()
            {
            }

            [MeasureTime(WarningThresholdMs = 1, ErrorThresholdMs = 5000)]
            public void SlowWork()
            {
            }

            [NoIntercept]
            [MeasureTime(WarningThresholdMs = 1, ErrorThresholdMs = 5000)]
            public void WorkWithoutIntercept()
            {
            }
        }
    }
}