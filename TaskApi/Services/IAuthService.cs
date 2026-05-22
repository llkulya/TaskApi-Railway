using TaskApi.Dto.Auth;
using TaskApi.Dto.Commands;
using TaskApi.Dto.Responses;

namespace TaskApi.Services
{
    /// <summary>
    /// Інтерфейс сервісу аутентифікації
    /// </summary>
    public interface IAuthService
    {
        /// <summary>Вхід у систему</summary>
        Task<AuthResponse> AuthenticateAsync(AuthRequest request);

        /// <summary>Реєстрація нового користувача</summary>
        Task<AuthResponse> RegisterAsync(RegisterRequest request);

        /// <summary>Оновлення токена</summary>
        Task<AuthResponse> RefreshTokenAsync(string refreshToken);

        /// <summary>Отримання даних поточного користувача</summary>
        Task<UserDto?> GetCurrentUserDataAsync(int userId);

        /// <summary>
        /// Внутрішня реєстрація користувача-співробітника з HR Service
        /// </summary>
        Task<RegisterEmployeeResponse> RegisterEmployeeAsync(RegisterEmployeeRequest request);
    }
}