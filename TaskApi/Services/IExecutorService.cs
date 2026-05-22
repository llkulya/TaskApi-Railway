using TaskApi.Models;
using TaskApi.Dto.Commands;
using TaskApi.Dto.Responses; 

namespace TaskApi.Services
{
    public interface IExecutorService
    {
        Task<List<ExecutorDto>> GetAllAsync();
        Task<ExecutorDto?> GetByIdAsync(int id);
        Task<ExecutorDto> CreateAsync(ExecutorCreateCommand command);
        Task<bool> DeleteAsync(int id);
    }
}