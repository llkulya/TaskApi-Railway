using TaskApi.Models;
using TaskApi.Repositories;
using TaskApi.Dto.Commands;

namespace TaskApi.Services
{
    public class ExecutorService : IExecutorService
    {
        private readonly IExecutorRepository _executorRepository;

        public ExecutorService(IExecutorRepository executorRepository)
        {
            _executorRepository = executorRepository;
        }

        public async Task<List<Executor>> GetAllAsync()
        {
            return await _executorRepository.GetAllAsync();
        }

        public async Task<Executor?> GetByIdAsync(int id)
        {
            return await _executorRepository.GetByIdAsync(id);
        }

        public async Task<Executor> CreateAsync(ExecutorCreateCommand command)
        {
            if (!Enum.TryParse<ExecutorRole>(command.Role, true, out var role))
                role = ExecutorRole.Developer;

            var executor = new Executor
            {
                FirstName = command.FullName,
                LastName = string.Empty,
                Email = command.Email,
                HireDate = DateTime.UtcNow,
                Role = role
            };

            return await _executorRepository.AddAsync(executor);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            return await _executorRepository.DeleteAsync(id);
        }
    }
}