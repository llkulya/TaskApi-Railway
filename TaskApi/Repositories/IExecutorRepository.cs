using TaskApi.Models;

namespace TaskApi.Repositories
{
    public interface IExecutorRepository : IBaseRepository<Executor>
    {
        Task<Executor?> GetByEmailAsync(string email);
    }
}