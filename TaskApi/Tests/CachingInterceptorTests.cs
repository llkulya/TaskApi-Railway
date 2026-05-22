using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using TaskApi.Attributes;
using TaskApi.Interceptors;

namespace TaskApi.Tests
{
    [TestFixture]
    public class CachingInterceptorTests
    {
        private IMemoryCache _memoryCache = null!;
        private Mock<ILogger<CachingInterceptor>> _mockLogger = null!;
        private CachingInterceptor _interceptor = null!;

        [SetUp]
        public void Setup()
        {
            _memoryCache = new MemoryCache(new MemoryCacheOptions());
            _mockLogger = new Mock<ILogger<CachingInterceptor>>();
            _interceptor = new CachingInterceptor(_memoryCache, _mockLogger.Object);
        }

        [TearDown]
        public void TearDown()
        {
            _memoryCache.Dispose();
        }

        [Test]
        public void Intercept_FirstCall_ShouldLogMissAndCacheResult()
        {
            // Arrange
            var methodInfo = typeof(FakeCacheService)
                .GetMethod(nameof(FakeCacheService.GetData))!;

            var invocation = new Mock<Castle.DynamicProxy.IInvocation>();

            invocation.SetupProperty(i => i.ReturnValue);

            invocation.Setup(i => i.Method).Returns(methodInfo);
            invocation.Setup(i => i.MethodInvocationTarget).Returns(methodInfo);
            invocation.Setup(i => i.InvocationTarget).Returns(new FakeCacheService());
            invocation.Setup(i => i.Arguments).Returns(new object[] { 1 });

            invocation.Setup(i => i.Proceed())
                .Callback(() => invocation.Object.ReturnValue = "result-1");

            // Act
            _interceptor.Intercept(invocation.Object);

            // Assert
            invocation.Verify(i => i.Proceed(), Times.Once);
            Assert.That(invocation.Object.ReturnValue, Is.EqualTo("result-1"));

            VerifyLog(LogLevel.Information, "AOP Cache: MISS", Times.Once());
        }

        [Test]
        public void Intercept_SecondCallWithSameParameters_ShouldReturnFromCache()
        {
            // Arrange
            var methodInfo = typeof(FakeCacheService)
                .GetMethod(nameof(FakeCacheService.GetData))!;

            var firstInvocation = CreateGetDataInvocation(methodInfo, 1, "result-1");
            var secondInvocation = CreateGetDataInvocation(methodInfo, 1, "result-2");

            // Act
            _interceptor.Intercept(firstInvocation.Object);
            _interceptor.Intercept(secondInvocation.Object);

            // Assert
            firstInvocation.Verify(i => i.Proceed(), Times.Once);

            // Другий виклик не має доходити до реального методу
            secondInvocation.Verify(i => i.Proceed(), Times.Never);

            // Має повернутися перший закешований результат
            Assert.That(secondInvocation.Object.ReturnValue, Is.EqualTo("result-1"));

            VerifyLog(LogLevel.Information, "AOP Cache: MISS", Times.Once());
            VerifyLog(LogLevel.Information, "AOP Cache: HIT", Times.Once());
        }

        [Test]
        public void Intercept_MutationMethod_ShouldClearCache()
        {
            // Arrange
            var getMethodInfo = typeof(FakeCacheService)
                .GetMethod(nameof(FakeCacheService.GetData))!;

            var createMethodInfo = typeof(FakeCacheService)
                .GetMethod(nameof(FakeCacheService.CreateData))!;

            var firstGet = CreateGetDataInvocation(getMethodInfo, 1, "result-1");
            var mutationInvocation = CreateMutationInvocation(createMethodInfo);
            var secondGet = CreateGetDataInvocation(getMethodInfo, 1, "result-2");

            // Act
            _interceptor.Intercept(firstGet.Object);          // MISS, кешує result-1
            _interceptor.Intercept(mutationInvocation.Object); // очищає кеш
            _interceptor.Intercept(secondGet.Object);         // знову MISS, бо кеш очищено

            // Assert
            firstGet.Verify(i => i.Proceed(), Times.Once);
            mutationInvocation.Verify(i => i.Proceed(), Times.Once);
            secondGet.Verify(i => i.Proceed(), Times.Once);

            Assert.That(secondGet.Object.ReturnValue, Is.EqualTo("result-2"));

            VerifyLog(LogLevel.Information, "кеш очищено", Times.Once());
        }

        [Test]
        public void Intercept_NoInterceptMethod_ShouldProceedWithoutCaching()
        {
            // Arrange
            var methodInfo = typeof(FakeCacheService)
                .GetMethod(nameof(FakeCacheService.GetWithoutCache))!;

            var invocation = new Mock<Castle.DynamicProxy.IInvocation>();

            invocation.SetupProperty(i => i.ReturnValue);

            invocation.Setup(i => i.Method).Returns(methodInfo);
            invocation.Setup(i => i.MethodInvocationTarget).Returns(methodInfo);
            invocation.Setup(i => i.InvocationTarget).Returns(new FakeCacheService());
            invocation.Setup(i => i.Arguments).Returns(Array.Empty<object>());

            invocation.Setup(i => i.Proceed())
                .Callback(() => invocation.Object.ReturnValue = "no-cache-result");

            // Act
            _interceptor.Intercept(invocation.Object);

            // Assert
            invocation.Verify(i => i.Proceed(), Times.Once);
            Assert.That(invocation.Object.ReturnValue, Is.EqualTo("no-cache-result"));

            VerifyLog(LogLevel.Information, "AOP Cache", Times.Never());
        }

        private Mock<Castle.DynamicProxy.IInvocation> CreateGetDataInvocation(
            System.Reflection.MethodInfo methodInfo,
            int id,
            string result)
        {
            var invocation = new Mock<Castle.DynamicProxy.IInvocation>();

            invocation.SetupProperty(i => i.ReturnValue);

            invocation.Setup(i => i.Method).Returns(methodInfo);
            invocation.Setup(i => i.MethodInvocationTarget).Returns(methodInfo);
            invocation.Setup(i => i.InvocationTarget).Returns(new FakeCacheService());
            invocation.Setup(i => i.Arguments).Returns(new object[] { id });

            invocation.Setup(i => i.Proceed())
                .Callback(() => invocation.Object.ReturnValue = result);

            return invocation;
        }

        private Mock<Castle.DynamicProxy.IInvocation> CreateMutationInvocation(
            System.Reflection.MethodInfo methodInfo)
        {
            var invocation = new Mock<Castle.DynamicProxy.IInvocation>();

            invocation.SetupProperty(i => i.ReturnValue);

            invocation.Setup(i => i.Method).Returns(methodInfo);
            invocation.Setup(i => i.MethodInvocationTarget).Returns(methodInfo);
            invocation.Setup(i => i.InvocationTarget).Returns(new FakeCacheService());
            invocation.Setup(i => i.Arguments).Returns(Array.Empty<object>());

            invocation.Setup(i => i.Proceed())
                .Callback(() => invocation.Object.ReturnValue = null);

            return invocation;
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

        private class FakeCacheService
        {
            [Cache(DurationSeconds = 60)]
            public string GetData(int id)
            {
                return $"data-{id}";
            }

            public void CreateData()
            {
            }

            [NoIntercept]
            [Cache(DurationSeconds = 60)]
            public string GetWithoutCache()
            {
                return "no-cache";
            }
        }
    }
}