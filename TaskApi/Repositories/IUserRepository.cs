using TaskApi.Models;

namespace TaskApi.Repositories
{
    /// <summary>
    /// Інтерфейс репозиторію користувачів
    /// </summary>
    public interface IUserRepository
    {
        Task<User?> GetByEmailAsync(string email);
        Task<User> AddAsync(User user);
        Task<User?> UpdateAsync(User user);
        Task<User?> GetByIdAsync(int id);
    }
}