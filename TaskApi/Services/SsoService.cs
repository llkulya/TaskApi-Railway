using TaskApi.Dto.Auth;
using TaskApi.Models;
using TaskApi.Repositories;

namespace TaskApi.Services
{
    /// <summary>
    /// Сервіс SSO аутентифікації через зовнішніх провайдерів (Google тощо)
    /// </summary>
    public class SsoService : ISsoService
    {
        private readonly IUserRepository _userRepository;
        private readonly IRefreshTokenService _refreshTokenService;

        public SsoService(
            IUserRepository userRepository,
            IRefreshTokenService refreshTokenService)
        {
            _userRepository = userRepository;
            _refreshTokenService = refreshTokenService;
        }

        /// <summary>
        /// Обробляє Google callback:
        /// — якщо користувач новий → створює акаунт
        /// — якщо існує → прив'язує GoogleId
        /// — генерує пару JWT токенів
        /// </summary>
        public async Task<AuthResponse> HandleGoogleCallbackAsync(
            string email, string googleId)
        {
            // Шукаємо існуючого користувача
            var user = await _userRepository.GetByEmailAsync(email);

            if (user == null)
            {
                // Перший вхід через Google — створюємо локальний акаунт
                user = new User
                {
                    Email = email,
                    // Випадковий пароль — через Google пароль не потрібен
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(
                        Guid.NewGuid().ToString()),
                    Role = "User",
                    GoogleId = googleId,
                    CreatedAt = DateTime.UtcNow,
                    LastLogin = DateTime.UtcNow
                };

                await _userRepository.AddAsync(user);
            }
            else
            {
                // Користувач існує — прив'язуємо GoogleId якщо ще не прив'язаний
                if (string.IsNullOrEmpty(user.GoogleId))
                    user.GoogleId = googleId;

                user.LastLogin = DateTime.UtcNow;
                await _userRepository.UpdateAsync(user);
            }

            // Генеруємо пару токенів — повертаємо стандартний AuthResponse
            return await _refreshTokenService.GenerateTokenPairAsync(user);
        }
    }
}