using TaskApi.Models;
using TaskApi.Dto.Commands;

namespace TaskApi.Services
{
    public interface IExecutorService
    {
        Task<List<Executor>> GetAllAsync();
        Task<Executor?> GetByIdAsync(int id);
        Task<Executor> CreateAsync(ExecutorCreateCommand command);
        Task<bool> DeleteAsync(int id);
    }
}
