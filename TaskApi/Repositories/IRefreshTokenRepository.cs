using TaskApi.Models;

namespace TaskApi.Repositories
{
    /// <summary>
    /// Інтерфейс репозиторію refresh токенів
    /// </summary>
    public interface IRefreshTokenRepository
    {
        /// <summary>Знайти токен за значенням</summary>
        Task<RefreshToken?> GetByTokenAsync(string token);

        /// <summary>Додати новий токен</summary>
        Task<RefreshToken> AddAsync(RefreshToken refreshToken);

        /// <summary>Оновити токен (відкликати)</summary>
        Task UpdateAsync(RefreshToken refreshToken);

        /// <summary>Отримати всі активні токени користувача</summary>
        Task<List<RefreshToken>> GetActiveTokensByUserIdAsync(int userId);

        /// <summary>Відкликати всі токени користувача (logout)</summary>
        Task RevokeAllUserTokensAsync(int userId);
    }
}