using TaskApi.Data;
using TaskApi.Models;

namespace TaskApi
{
    /// <summary>
    /// Ініціалізація тестових даних при першому запуску
    /// </summary>
    public static class SeedData
    {
        public static void Initialize(ApplicationDbContext context)
        {
            // Якщо користувачі вже є — нічого не робимо
            if (context.Users.Any())
                return;

            // Тестовий адміністратор
            var admin = new User
            {
                Email = "admin@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                Role = "Admin",
                CreatedAt = DateTime.UtcNow
            };

            // Тестовий користувач
            var user = new User
            {
                Email = "user@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("user123"),
                Role = "User",
                CreatedAt = DateTime.UtcNow
            };

            context.Users.AddRange(admin, user);
            context.SaveChanges();

            Console.WriteLine("✅ SeedData: тестових користувачів створено");
        }
    }
}