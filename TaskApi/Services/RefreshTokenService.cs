using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using TaskApi.Configuration;
using TaskApi.Dto.Auth;
using TaskApi.Exceptions;
using TaskApi.Models;
using TaskApi.Repositories;

namespace TaskApi.Services
{
    /// <summary>
    /// Сервіс для роботи з refresh токенами
    /// </summary>
    public class RefreshTokenService : IRefreshTokenService
    {
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IUserRepository _userRepository;
        private readonly JwtSettings _jwtSettings;

        // Максимальна кількість активних токенів на користувача
        private const int MaxActiveTokensPerUser = 5;

        public RefreshTokenService(
            IRefreshTokenRepository refreshTokenRepository,
            IUserRepository userRepository,
            JwtSettings jwtSettings)
        {
            _refreshTokenRepository = refreshTokenRepository;
            _userRepository = userRepository;
            _jwtSettings = jwtSettings;
        }

        /// <summary>
        /// Генерує пару токенів: access (JWT) + refresh
        /// </summary>
        public async Task<AuthResponse> GenerateTokenPairAsync(User user)
        {
            // Перевірка кількості активних токенів
            var activeTokens = await _refreshTokenRepository
                .GetActiveTokensByUserIdAsync(user.Id);

            // Якщо перевищено ліміт — відкликаємо найстаріший
            if (activeTokens.Count >= MaxActiveTokensPerUser)
            {
                var oldest = activeTokens.OrderBy(t => t.CreatedAt).First();
                oldest.IsRevoked = true;
                await _refreshTokenRepository.UpdateAsync(oldest);
            }

            // Генеруємо access токен (JWT)
            var accessToken = GenerateJwtToken(user);

            // Генеруємо refresh токен (криптографічно безпечний)
            var refreshTokenValue = GenerateSecureRefreshToken();

            // Зберігаємо refresh токен у БД
            var refreshToken = new RefreshToken
            {
                Token = refreshTokenValue,
                UserId = user.Id,
                ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryDays),
                CreatedAt = DateTime.UtcNow,
                IsRevoked = false
            };

            await _refreshTokenRepository.AddAsync(refreshToken);

            return new AuthResponse
            {
                Token = accessToken,
                TokenType = "Bearer",
                ExpiresIn = _jwtSettings.TokenExpiryMinutes * 60,
                RefreshToken = refreshTokenValue,
                User = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    Role = user.Role,
                    CreatedAt = user.CreatedAt
                }
            };
        }

        /// <summary>
        /// Оновлює пару токенів за refresh токеном
        /// </summary>
        public async Task<AuthResponse> RefreshAsync(string refreshToken)
        {
            // Знаходимо токен у БД
            var storedToken = await _refreshTokenRepository
                .GetByTokenAsync(refreshToken);

            // Перевірка існування
            if (storedToken == null)
                throw new UnauthorizedException("Refresh token not found");

            // Перевірка чи не відкликаний
            if (storedToken.IsRevoked)
                throw new UnauthorizedException("Refresh token has been revoked");

            // Перевірка терміну дії
            if (storedToken.ExpiresAt <= DateTime.UtcNow)
                throw new UnauthorizedException("Refresh token has expired");

            // Отримуємо користувача
            var user = storedToken.User
                ?? await _userRepository.GetByIdAsync(storedToken.UserId);

            if (user == null)
                throw new UnauthorizedException("User not found");

            // Відкликаємо старий refresh токен
            storedToken.IsRevoked = true;
            await _refreshTokenRepository.UpdateAsync(storedToken);

            // Генеруємо нову пару токенів
            return await GenerateTokenPairAsync(user);
        }

        /// <summary>
        /// Відкликає конкретний refresh токен (logout з одного пристрою)
        /// </summary>
        public async Task RevokeAsync(string refreshToken)
        {
            var storedToken = await _refreshTokenRepository
                .GetByTokenAsync(refreshToken);

            if (storedToken == null)
                throw new UnauthorizedException("Refresh token not found");

            storedToken.IsRevoked = true;
            await _refreshTokenRepository.UpdateAsync(storedToken);
        }

        /// <summary>
        /// Відкликає всі токени користувача (logout з усіх пристроїв)
        /// </summary>
        public async Task RevokeAllAsync(int userId)
        {
            await _refreshTokenRepository.RevokeAllUserTokensAsync(userId);
        }

        /// <summary>
        /// Генерація JWT access токена
        /// </summary>
        private string GenerateJwtToken(User user)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_jwtSettings.Secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwtSettings.TokenExpiryMinutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <summary>
        /// Генерація криптографічно безпечного refresh токена
        /// </summary>
        private string GenerateSecureRefreshToken()
        {
            var randomBytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }
    }
}