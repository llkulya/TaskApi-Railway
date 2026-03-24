using TaskApi.Models;

namespace TaskApi.Repositories
{
    public class ProjectManagerRepository : IProjectManagerRepository
    {
        private readonly List<ProjectManager> _managers = new();

        public ProjectManagerRepository()
        {
            // Тестові дані
            _managers.AddRange(new List<ProjectManager>
            {
                new ProjectManager
                {
                    Id = 1,
                    FirstName = "Іван",
                    LastName = "Петренко",
                    Email = "ivan@example.com"
                },
                new ProjectManager
                {
                    Id = 2,
                    FirstName = "Марія",
                    LastName = "Коваленко",
                    Email = "maria@example.com"
                }
            });
        }

        public Task<List<ProjectManager>> GetAllAsync()
        {
            return Task.FromResult(_managers.ToList());
        }

        public Task<ProjectManager?> GetByIdAsync(int id)
        {
            var manager = _managers.FirstOrDefault(m => m.Id == id);
            return Task.FromResult<ProjectManager?>(manager);
        }

        public Task<ProjectManager> AddAsync(ProjectManager manager)
        {
            var maxId = _managers.Any() ? _managers.Max(m => m.Id) : 0;
            manager.Id = maxId + 1;
            _managers.Add(manager);
            return Task.FromResult(manager);
        }

        public Task<bool> DeleteAsync(int id)
        {
            var manager = _managers.FirstOrDefault(m => m.Id == id);
            if (manager == null) return Task.FromResult(false);
            _managers.Remove(manager);
            return Task.FromResult(true);
        }
    }
}