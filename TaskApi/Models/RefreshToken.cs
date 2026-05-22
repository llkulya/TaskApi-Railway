namespace TaskApi.Models
{
    /// <summary>
    /// Refresh токен для оновлення JWT
    /// </summary>
    public class RefreshToken
    {
        public int Id { get; set; }

        /// <summary>Значення токена (криптографічно безпечний рядок)</summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>Дата закінчення терміну дії</summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>Чи відкликаний токен</summary>
        public bool IsRevoked { get; set; } = false;

        /// <summary>Дата створення</summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>ID користувача якому належить токен</summary>
        public int UserId { get; set; }

        /// <summary>Навігаційна властивість</summary>
        public User? User { get; set; }

        /// <summary>Чи токен ще дійсний</summary>
        public bool IsActive => !IsRevoked && ExpiresAt > DateTime.UtcNow;
    }
}