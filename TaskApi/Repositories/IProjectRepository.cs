using TaskApi.Models;

namespace TaskApi.Repositories
{
    public interface IProjectRepository : IBaseRepository<Project>
    {
        Task<List<Project>> GetActiveProjectsAsync();
    }
}