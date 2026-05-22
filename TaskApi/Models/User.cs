namespace TaskApi.Models
{
    /// <summary>
    /// Сутність користувача системи
    /// </summary>
    public class User
    {
        public int Id { get; set; }

        /// <summary>Email (унікальний)</summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>Хеш пароля (BCrypt)</summary>
        public string PasswordHash { get; set; } = string.Empty;

        /// <summary>Роль: "User" або "Admin"</summary>
        public string Role { get; set; } = "User";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastLogin { get; set; }
        /// <summary>Google ID для SSO (null якщо звичайна реєстрація)</summary>
        public string? GoogleId { get; set; }
    }
}