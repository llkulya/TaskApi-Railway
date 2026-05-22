using System.ComponentModel.DataAnnotations;
using TaskApi.Attributes;

namespace TaskApi.Dto.Auth
{
    /// <summary>
    /// Запит на вхід у систему
    /// </summary>
    public class AuthRequest
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;
        [Required(ErrorMessage = "Password is required")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        [SensitiveData]
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>
    /// Відповідь після успішної аутентифікації
    /// </summary>
    public class AuthResponse
    {
        public string Token { get; set; } = string.Empty;
        public string TokenType { get; set; } = "Bearer";
        public int ExpiresIn { get; set; }
        public string RefreshToken { get; set; } = string.Empty;
        public UserDto User { get; set; } = new();
    }

    /// <summary>
    /// DTO користувача (без пароля!)
    /// </summary>
    public class UserDto
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Запит на реєстрацію
    /// </summary>
    public class RegisterRequest
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        [SensitiveData]
        public string Password { get; set; } = string.Empty;

        [Compare("Password", ErrorMessage = "Passwords do not match")]
        [SensitiveData]
        public string ConfirmPassword { get; set; } = string.Empty;

        public string Role { get; set; } = "User";
    }

    /// <summary>
    /// Запит на оновлення токена
    /// </summary>
    public class RefreshTokenRequest
    {
        public string RefreshToken { get; set; } = string.Empty;
    }
}