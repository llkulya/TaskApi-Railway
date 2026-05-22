using TaskApi.Dto.Auth;

namespace TaskApi.Services
{
    /// <summary>
    /// Інтерфейс сервісу SSO аутентифікації
    /// </summary>
    public interface ISsoService
    {
        /// <summary>
        /// Обробка callback від Google — пошук або створення користувача,
        /// генерація JWT токенів
        /// </summary>
        Task<AuthResponse> HandleGoogleCallbackAsync(
            string email, string googleId);
    }
}