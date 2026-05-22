using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using TaskApi.Configuration;
using TaskApi.Dto.Auth;
using TaskApi.Exceptions;
using TaskApi.Models;
using TaskApi.Repositories;
using TaskApi.Services;

namespace TaskApi.Tests
{
    [TestFixture]
    public class AuthServiceTests
    {
        private Mock<IUserRepository> _mockUserRepository = null!;
        private Mock<IRefreshTokenService> _mockRefreshTokenService = null!;
        private Mock<ILogger<AuthService>> _mockLogger = null!;
        private Mock<IHttpContextAccessor> _mockHttpContextAccessor = null!;
        private Mock<IEmailService> _mockEmailService = null!;
        private AuthService _authService = null!;
        private JwtSettings _jwtSettings = null!;

        [SetUp]
        public void Setup()
        {
            _mockUserRepository = new Mock<IUserRepository>();
            _mockRefreshTokenService = new Mock<IRefreshTokenService>();
            _mockLogger = new Mock<ILogger<AuthService>>();
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            _mockEmailService = new Mock<IEmailService>();

            _jwtSettings = new JwtSettings
            {
                Secret = "test-secret-key-minimum-32-characters-long",
                Issuer = "TestApi",
                Audience = "TestUsers",
                TokenExpiryMinutes = 60,
                RefreshTokenExpiryDays = 7
            };

            var httpContext = new DefaultHttpContext();
            httpContext.Connection.RemoteIpAddress =
                System.Net.IPAddress.Parse("192.168.1.100");

            _mockEmailService
    .Setup(x => x.SendEmployeeCredentialsAsync(
        It.IsAny<string>(),
        It.IsAny<string>()))
    .Returns(Task.CompletedTask);

            _mockHttpContextAccessor
                .Setup(x => x.HttpContext)
                .Returns(httpContext);

            _authService = new AuthService(
    _mockUserRepository.Object,
    _mockRefreshTokenService.Object,
    _jwtSettings,
    _mockLogger.Object,
    _mockHttpContextAccessor.Object,
    _mockEmailService.Object);

        }

        [Test]
        public async Task AuthenticateAsync_ValidCredentials_LogsSuccessfulLogin()
        {
            // Arrange
            var request = new AuthRequest
            {
                Email = "admin@example.com",
                Password = "admin123"
            };

            var user = new User
            {
                Id = 1,
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Role = "Admin",
                CreatedAt = DateTime.UtcNow
            };

            var authResponse = new AuthResponse
            {
                Token = "test-token",
                RefreshToken = "test-refresh-token",
                ExpiresIn = 3600,
                User = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    Role = user.Role
                }
            };

            _mockUserRepository
                .Setup(r => r.GetByEmailAsync(request.Email))
                .ReturnsAsync(user);

            _mockUserRepository
                .Setup(r => r.UpdateAsync(It.IsAny<User>()))
                .ReturnsAsync(user);

            _mockRefreshTokenService
                .Setup(r => r.GenerateTokenPairAsync(user))
                .ReturnsAsync(authResponse);

            // Act
            var result = await _authService.AuthenticateAsync(request);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Token, Is.EqualTo("test-token"));

            VerifyLog(LogLevel.Information, "Attempting login", Times.Once());
            VerifyLog(LogLevel.Information, "logged in successfully", Times.Once());

            _mockUserRepository.Verify(
                r => r.UpdateAsync(It.Is<User>(u => u.LastLogin != null)),
                Times.Once);
        }

        [Test]
        public void AuthenticateAsync_UserNotFound_LogsWarning()
        {
            // Arrange
            var request = new AuthRequest
            {
                Email = "notfound@example.com",
                Password = "password123"
            };

            _mockUserRepository
                .Setup(r => r.GetByEmailAsync(request.Email))
                .ReturnsAsync((User?)null);

            // Act & Assert
            Assert.ThrowsAsync<UnauthorizedException>(
                async () => await _authService.AuthenticateAsync(request));

            VerifyLog(LogLevel.Warning, "Login failed", Times.AtLeastOnce());
        }

        [Test]
        public void AuthenticateAsync_InvalidPassword_LogsWarning()
        {
            // Arrange
            var request = new AuthRequest
            {
                Email = "admin@example.com",
                Password = "wrong-password"
            };

            var user = new User
            {
                Id = 1,
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("correct-password"),
                Role = "Admin",
                CreatedAt = DateTime.UtcNow
            };

            _mockUserRepository
                .Setup(r => r.GetByEmailAsync(request.Email))
                .ReturnsAsync(user);

            // Act & Assert
            Assert.ThrowsAsync<UnauthorizedException>(
                async () => await _authService.AuthenticateAsync(request));

            VerifyLog(LogLevel.Warning, "invalid password", Times.AtLeastOnce());
        }

        [Test]
        public async Task AuthenticateAsync_MoreThanFiveFailedAttempts_LogsCritical()
        {
            // Arrange
            var request = new AuthRequest
            {
                Email = "attack@example.com",
                Password = "wrong-password"
            };

            var user = new User
            {
                Id = 2,
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("correct-password"),
                Role = "User",
                CreatedAt = DateTime.UtcNow
            };

            _mockUserRepository
                .Setup(r => r.GetByEmailAsync(request.Email))
                .ReturnsAsync(user);

            // Act: 6 неправильних спроб входу
            for (int i = 0; i < 6; i++)
            {
                try
                {
                    await _authService.AuthenticateAsync(request);
                }
                catch (UnauthorizedException)
                {
                    // Очікувана помилка, бо пароль неправильний
                }
            }

            // Assert
            VerifyLog(LogLevel.Critical, "Suspicious activity detected", Times.AtLeastOnce());
        }

        [Test]
        public async Task RegisterAsync_NewUser_LogsSuccessfulRegistration()
        {
            // Arrange
            var request = new RegisterRequest
            {
                Email = "newuser@example.com",
                Password = "password123",
                ConfirmPassword = "password123",
                Role = "User"
            };

            var createdUser = new User
            {
                Id = 5,
                Email = request.Email,
                Role = request.Role,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                CreatedAt = DateTime.UtcNow
            };

            var authResponse = new AuthResponse
            {
                Token = "new-token",
                RefreshToken = "new-refresh-token",
                User = new UserDto
                {
                    Id = createdUser.Id,
                    Email = createdUser.Email,
                    Role = createdUser.Role
                }
            };

            _mockUserRepository
                .Setup(r => r.GetByEmailAsync(request.Email))
                .ReturnsAsync((User?)null);

            _mockUserRepository
                .Setup(r => r.AddAsync(It.IsAny<User>()))
                .ReturnsAsync(createdUser);

            _mockRefreshTokenService
                .Setup(r => r.GenerateTokenPairAsync(It.IsAny<User>()))
                .ReturnsAsync(authResponse);

            // Act
            var result = await _authService.RegisterAsync(request);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Token, Is.EqualTo("new-token"));

            _mockUserRepository.Verify(
                r => r.AddAsync(It.IsAny<User>()),
                Times.Once);

            VerifyLog(LogLevel.Information, "registered successfully", Times.AtLeastOnce());
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