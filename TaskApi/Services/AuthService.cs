using System.ComponentModel.DataAnnotations;
using TaskApi.Configuration;
using TaskApi.Dto.Auth;
using System.Collections.Concurrent;
using TaskApi.Exceptions;
using TaskApi.Utilities;
using TaskApi.Models;
using TaskApi.Dto.Commands;
using TaskApi.Dto.Responses;
using System.Security.Cryptography;
using TaskApi.Repositories;

namespace TaskApi.Services
{
    /// <summary>
    /// Сервіс аутентифікації з семантичним логуванням
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly JwtSettings _jwtSettings;
        private readonly ILogger<AuthService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private static readonly ConcurrentDictionary<string, List<DateTime>> _failedLoginAttempts = new();
        private readonly IEmailService _emailService;

        public AuthService(
            IUserRepository userRepository,
            IRefreshTokenService refreshTokenService,
            JwtSettings jwtSettings,
            ILogger<AuthService> logger,
            IHttpContextAccessor httpContextAccessor,
            IEmailService emailService)
        {
            _userRepository = userRepository;
            _refreshTokenService = refreshTokenService;
            _jwtSettings = jwtSettings;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            _emailService = emailService;
        }

        private string GetIpAddress() =>
            _httpContextAccessor.HttpContext?
                .Connection.RemoteIpAddress?.ToString() ?? "unknown";

        /// <summary>
        /// Аутентифікація користувача
        /// </summary>
        public async Task<AuthResponse> AuthenticateAsync(AuthRequest request)
        {
            var ipAddress = GetIpAddress();
            var maskedEmail = MaskingHelper.MaskEmail(request.Email);

            try
            {
                _logger.LogInformation(
    "Attempting login for user {Email} from IP {IpAddress}",
    maskedEmail, ipAddress);

                var user = await _userRepository.GetByEmailAsync(request.Email);
                if (user == null)
                {
                    _logger.LogWarning(
     "Login failed - user {Email} not found from IP {IpAddress}",
     maskedEmail, ipAddress);

                    RecordFailedLogin(request.Email, ipAddress);

                    throw new UnauthorizedException("Invalid email or password");
                }

                if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                {
                    _logger.LogWarning(
    "Login failed - invalid password for {Email} from IP {IpAddress}",
    maskedEmail, ipAddress);

                    RecordFailedLogin(request.Email, ipAddress);

                    throw new UnauthorizedException("Invalid email or password");
                }

                user.LastLogin = DateTime.UtcNow;
                await _userRepository.UpdateAsync(user);

                _logger.LogInformation(
    "User {UserId} ({Email}) logged in successfully from IP {IpAddress}. Role: {Role}",
    user.Id, maskedEmail, ipAddress, user.Role);

                ClearFailedLoginAttempts(request.Email, ipAddress);
                return await _refreshTokenService.GenerateTokenPairAsync(user);
            }
            catch (UnauthorizedException ex)
            {
                _logger.LogWarning(ex,
    "Unauthorized login attempt for {Email} from {IpAddress}",
    maskedEmail, ipAddress);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
    "Unexpected error during login for {Email} from {IpAddress}",
    maskedEmail, ipAddress);
                throw;
            }
        }

        /// <summary>
        /// Реєстрація нового користувача
        /// </summary>
        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            var ipAddress = GetIpAddress();
            var maskedEmail = MaskingHelper.MaskEmail(request.Email);

            try
            {
                _logger.LogInformation(
                    "Attempting registration for {Email} from IP {IpAddress}",
                   maskedEmail, ipAddress);

                var existingUser = await _userRepository.GetByEmailAsync(request.Email);
                if (existingUser != null)
                {
                    _logger.LogWarning(
                        "Registration failed - email {Email} already exists from IP {IpAddress}",
                        maskedEmail, ipAddress);

                    throw new ValidationException("User with this email already exists");
                }

                var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

                var user = new User
                {
                    Email = request.Email,
                    PasswordHash = passwordHash,
                    Role = request.Role,
                    CreatedAt = DateTime.UtcNow
                };

                await _userRepository.AddAsync(user);

                _logger.LogInformation(
                    "User {UserId} ({Email}) registered successfully from IP {IpAddress}. Role: {Role}",
                    user.Id, user.Email, ipAddress, user.Role);

                return await _refreshTokenService.GenerateTokenPairAsync(user);
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex,
                    "Registration validation failed for {Email} from {IpAddress}",
                    maskedEmail, ipAddress);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Unexpected error during registration for {Email} from {IpAddress}",
                    maskedEmail, ipAddress);
                throw;
            }
        }

        /// <summary>
        /// Оновлення токена через refresh токен
        /// </summary>
        public async Task<AuthResponse> RefreshTokenAsync(string refreshToken)
        {
            try
            {
                _logger.LogInformation("Attempting token refresh");

                var response = await _refreshTokenService.RefreshAsync(refreshToken);

                _logger.LogInformation(
                    "Token refreshed successfully for user {UserId}",
                    response.User.Id);

                return response;
            }
            catch (UnauthorizedException ex)
            {
                _logger.LogWarning(ex, "Token refresh failed - unauthorized");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during token refresh");
                throw;
            }
        }

        /// <summary>
        /// Отримання даних поточного користувача
        /// </summary>
        public async Task<UserDto?> GetCurrentUserDataAsync(int userId)
        {
            try
            {
                _logger.LogDebug(
                    "Fetching current user data for UserId {UserId}",
                    userId);

                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning(
                        "User {UserId} not found when fetching current user data",
                        userId);
                    return null;
                }

                return new UserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    Role = user.Role,
                    CreatedAt = user.CreatedAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Unexpected error fetching user data for {UserId}",
                    userId);
                throw;
            }
        }

        /// <summary>
        /// Внутрішня реєстрація співробітника, який прийшов з HR Service
        /// </summary>
        public async Task<RegisterEmployeeResponse> RegisterEmployeeAsync(
            RegisterEmployeeRequest request)
        {
            var maskedEmail = MaskingHelper.MaskEmail(request.Email);

            try
            {
                _logger.LogInformation(
                    "HR employee registration started. ServiceName: {ServiceName}, MethodName: {MethodName}, EmployeeEmail: {EmployeeEmail}",
                    "TaskService",
                    nameof(RegisterEmployeeAsync),
                    maskedEmail);

                var existingUser = await _userRepository.GetByEmailAsync(request.Email);

                if (existingUser != null)
                {
                    _logger.LogWarning(
                        "HR employee registration failed. User already exists. ServiceName: {ServiceName}, MethodName: {MethodName}, Success: {Success}, EmployeeEmail: {EmployeeEmail}",
                        "TaskService",
                        nameof(RegisterEmployeeAsync),
                        false,
                        maskedEmail);

                    throw new ValidationException("User with this email already exists");
                }

                var temporaryPassword = GenerateTemporaryPassword();
                var passwordHash = BCrypt.Net.BCrypt.HashPassword(temporaryPassword);

                var user = new User
                {
                    Email = request.Email,
                    PasswordHash = passwordHash,
                    Role = "User",
                    CreatedAt = DateTime.UtcNow
                };

                await _userRepository.AddAsync(user);

                await _emailService.SendEmployeeCredentialsAsync(
    request.Email,
    temporaryPassword);

                _logger.LogInformation(
                    "HR employee registered successfully. ServiceName: {ServiceName}, MethodName: {MethodName}, Success: {Success}, EmployeeEmail: {EmployeeEmail}, UserId: {UserId}, Department: {Department}, Position: {Position}",
                    "TaskService",
                    nameof(RegisterEmployeeAsync),
                    true,
                    maskedEmail,
                    user.Id,
                    request.Department,
                    request.Position);

                return new RegisterEmployeeResponse
                {
                    UserId = user.Id,
                    Email = user.Email,
                    TemporaryPassword = temporaryPassword,
                    CreatedAt = user.CreatedAt,
                    Message = "Employee user account created successfully"
                };
            }
            catch (ValidationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "HR employee registration failed. ServiceName: {ServiceName}, MethodName: {MethodName}, Success: {Success}, EmployeeEmail: {EmployeeEmail}",
                    "TaskService",
                    nameof(RegisterEmployeeAsync),
                    false,
                    maskedEmail);

                throw;
            }
        }

        private static string GenerateTemporaryPassword()
        {
            var bytes = RandomNumberGenerator.GetBytes(12);
            var base64 = Convert.ToBase64String(bytes)
                .Replace("+", "A")
                .Replace("/", "b")
                .Replace("=", "C");

            return $"{base64[..12]}a1!";
        }

        private void RecordFailedLogin(string email, string ipAddress)
        {
            var key = $"{email}:{ipAddress}";
            var now = DateTime.UtcNow;
            var windowStart = now.AddMinutes(-15);

            var attempts = _failedLoginAttempts.GetOrAdd(key, _ => new List<DateTime>());

            lock (attempts)
            {
                attempts.RemoveAll(x => x < windowStart);
                attempts.Add(now);

                if (attempts.Count > 5)
                {
                    var maskedEmail = MaskingHelper.MaskEmail(email);
                    _logger.LogCritical(
                        "Suspicious activity detected: {FailedAttempts} failed login attempts for {Email} from IP {IpAddress} within 15 minutes",
                        attempts.Count,
                        maskedEmail,
                        ipAddress);
                }
            }
        }

        private void ClearFailedLoginAttempts(string email, string ipAddress)
        {
            var key = $"{email}:{ipAddress}";
            _failedLoginAttempts.TryRemove(key, out _);
        }
    }
}