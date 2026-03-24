using TaskApi.Models;

namespace TaskApi.Repositories
{
    public interface IProjectManagerRepository
    {
        Task<List<ProjectManager>> GetAllAsync();
        Task<ProjectManager?> GetByIdAsync(int id);
        Task<ProjectManager> AddAsync(ProjectManager manager);
        Task<bool> DeleteAsync(int id);
    }
}