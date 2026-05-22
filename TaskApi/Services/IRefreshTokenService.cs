using TaskApi.Dto.Auth;
using TaskApi.Models;

namespace TaskApi.Services
{
    /// <summary>
    /// Інтерфейс сервісу refresh токенів
    /// </summary>
    public interface IRefreshTokenService
    {
        /// <summary>Генерувати нову пару токенів (access + refresh)</summary>
        Task<AuthResponse> GenerateTokenPairAsync(User user);

        /// <summary>Оновити токени за refresh токеном</summary>
        Task<AuthResponse> RefreshAsync(string refreshToken);

        /// <summary>Відкликати refresh токен (logout)</summary>
        Task RevokeAsync(string refreshToken);

        /// <summary>Відкликати всі токени користувача</summary>
        Task RevokeAllAsync(int userId);
    }
}