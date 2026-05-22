namespace TaskApi.Configuration
{
    /// <summary>
    /// Налаштування JWT токенів
    /// </summary>
    public class JwtSettings
    {
        public string Secret { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public int TokenExpiryMinutes { get; set; } = 60;
        public int RefreshTokenExpiryDays { get; set; } = 7;
    }
}