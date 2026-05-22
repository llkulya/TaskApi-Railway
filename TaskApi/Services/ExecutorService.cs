using TaskApi.Models;
using TaskApi.Repositories;
using TaskApi.Dto.Commands;
using TaskApi.Dto.Responses;

namespace TaskApi.Services
{
    public class ExecutorService : IExecutorService
    {
        private readonly IExecutorRepository _executorRepository;

        public ExecutorService(IExecutorRepository executorRepository)
        {
            _executorRepository = executorRepository;
        }

        // 1. Повертаємо список DTO замість моделей БД
        public async Task<List<ExecutorDto>> GetAllAsync()
        {
            var executors = await _executorRepository.GetAllAsync();
            return executors.Select(e => MapToDto(e)).ToList();
        }

        // 2. Повертаємо один DTO
        public async Task<ExecutorDto?> GetByIdAsync(int id)
        {
            var executor = await _executorRepository.GetByIdAsync(id);
            return executor == null ? null : MapToDto(executor);
        }

        // 3. Логіка створення з розділенням імені та прізвища
        public async Task<ExecutorDto> CreateAsync(ExecutorCreateCommand command)
        {
            if (!Enum.TryParse<ExecutorRole>(command.Role, true, out var role))
                role = ExecutorRole.Developer;

            var executor = new Executor
            {
                // Використовуємо нові поля з команди (FirstName/LastName)
                FirstName = command.FirstName,
                LastName = command.LastName,
                Email = command.Email,
                HireDate = DateTime.UtcNow,
                Role = role
            };

            var created = await _executorRepository.AddAsync(executor);
            return MapToDto(created);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            return await _executorRepository.DeleteAsync(id);
        }

        // 4. Допоміжний метод для перетворення (Mapping)
        private ExecutorDto MapToDto(Executor executor)
        {
            return new ExecutorDto
            {
                Id = executor.Id,
                FirstName = executor.FirstName,
                LastName = executor.LastName,
                Email = executor.Email,
                Role = executor.Role.ToString(), // Перетворюємо Enum у текст для Swagger
                HireDate = executor.HireDate
            };
        }
    }
}