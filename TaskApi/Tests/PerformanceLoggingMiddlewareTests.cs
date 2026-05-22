using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using TaskApi.Middleware;

namespace TaskApi.Tests
{
    [TestFixture]
    public class PerformanceLoggingMiddlewareTests
    {
        private Mock<ILogger<PerformanceLoggingMiddleware>> _mockLogger = null!;

        [SetUp]
        public void Setup()
        {
            _mockLogger = new Mock<ILogger<PerformanceLoggingMiddleware>>();
        }

        [Test]
        public async Task InvokeAsync_FastRequest_LogsInformation()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Method = "GET";
            context.Request.Path = "/api/test";

            RequestDelegate next = ctx => Task.CompletedTask;

            var middleware = new PerformanceLoggingMiddleware(
                next,
                _mockLogger.Object);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            VerifyLog(
                LogLevel.Information,
                "Request GET /api/test took",
                Times.Once());
        }

        [Test]
        public async Task InvokeAsync_SlowRequest_LogsWarning()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Method = "POST";
            context.Request.Path = "/api/slow-test";

            RequestDelegate next = async ctx =>
            {
                // Імітація повільного запиту більше 2 секунд
                await Task.Delay(2100);
            };

            var middleware = new PerformanceLoggingMiddleware(
                next,
                _mockLogger.Object);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            VerifyLog(
                LogLevel.Warning,
                "Request POST /api/slow-test took",
                Times.Once());
        }

        [Test]
        public async Task InvokeAsync_WhenNextMiddlewareThrows_ExceptionIsPropagated()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Method = "GET";
            context.Request.Path = "/api/error-test";

            RequestDelegate next = ctx =>
            {
                throw new InvalidOperationException("Test middleware exception");
            };

            var middleware = new PerformanceLoggingMiddleware(
                next,
                _mockLogger.Object);

            // Act & Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(
                async () => await middleware.InvokeAsync(context));

            Assert.That(ex!.Message, Is.EqualTo("Test middleware exception"));
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
    }
}