using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using TaskApi.Attributes;
using TaskApi.Interceptors;

namespace TaskApi.Tests
{
    [TestFixture]
    public class ExceptionHandlingInterceptorTests
    {
        private Mock<ILogger<ExceptionHandlingInterceptor>> _mockLogger = null!;
        private ExceptionHandlingInterceptor _interceptor = null!;

        [SetUp]
        public void Setup()
        {
            _mockLogger = new Mock<ILogger<ExceptionHandlingInterceptor>>();
            _interceptor = new ExceptionHandlingInterceptor(_mockLogger.Object);
        }

        [Test]
        public void Intercept_WhenMethodThrows_ShouldLogErrorAndRethrow()
        {
            // Arrange
            var methodInfo = typeof(FakeExceptionService)
                .GetMethod(nameof(FakeExceptionService.ThrowError))!;

            var invocation = new Mock<Castle.DynamicProxy.IInvocation>();

            invocation.Setup(i => i.Method).Returns(methodInfo);
            invocation.Setup(i => i.MethodInvocationTarget).Returns(methodInfo);
            invocation.Setup(i => i.InvocationTarget).Returns(new FakeExceptionService());
            invocation.Setup(i => i.Arguments).Returns(Array.Empty<object>());

            invocation.Setup(i => i.Proceed())
                .Throws(new InvalidOperationException("Test exception"));

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() =>
                _interceptor.Intercept(invocation.Object));

            Assert.That(ex!.Message, Is.EqualTo("Test exception"));

            VerifyLog(
                LogLevel.Error,
                "AOP Exception",
                Times.Once());
        }

        [Test]
        public void Intercept_WhenMethodDoesNotThrow_ShouldProceedWithoutErrorLog()
        {
            // Arrange
            var methodInfo = typeof(FakeExceptionService)
                .GetMethod(nameof(FakeExceptionService.DoWork))!;

            var invocation = new Mock<Castle.DynamicProxy.IInvocation>();

            invocation.Setup(i => i.Method).Returns(methodInfo);
            invocation.Setup(i => i.MethodInvocationTarget).Returns(methodInfo);
            invocation.Setup(i => i.InvocationTarget).Returns(new FakeExceptionService());
            invocation.Setup(i => i.Arguments).Returns(Array.Empty<object>());

            // Act
            _interceptor.Intercept(invocation.Object);

            // Assert
            invocation.Verify(i => i.Proceed(), Times.Once);

            VerifyLog(
                LogLevel.Error,
                "AOP Exception",
                Times.Never());
        }

        [Test]
        public void Intercept_MethodWithNoIntercept_ShouldProceedWithoutErrorLog()
        {
            // Arrange
            var methodInfo = typeof(FakeExceptionService)
                .GetMethod(nameof(FakeExceptionService.NoInterceptError))!;

            var invocation = new Mock<Castle.DynamicProxy.IInvocation>();

            invocation.Setup(i => i.Method).Returns(methodInfo);
            invocation.Setup(i => i.MethodInvocationTarget).Returns(methodInfo);
            invocation.Setup(i => i.InvocationTarget).Returns(new FakeExceptionService());
            invocation.Setup(i => i.Arguments).Returns(Array.Empty<object>());

            invocation.Setup(i => i.Proceed())
                .Throws(new InvalidOperationException("NoIntercept exception"));

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() =>
                _interceptor.Intercept(invocation.Object));

            Assert.That(ex!.Message, Is.EqualTo("NoIntercept exception"));

            VerifyLog(
                LogLevel.Error,
                "AOP Exception",
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

        private class FakeExceptionService
        {
            public void DoWork()
            {
            }

            public void ThrowError()
            {
                throw new InvalidOperationException("Test exception");
            }

            [NoIntercept]
            public void NoInterceptError()
            {
                throw new InvalidOperationException("NoIntercept exception");
            }
        }
    }
}