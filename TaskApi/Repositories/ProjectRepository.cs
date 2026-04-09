using Microsoft.EntityFrameworkCore;
using TaskApi.Data;
using TaskApi.Models;

namespace TaskApi.Repositories
{
    public class ProjectRepository : BaseRepository<Project>, IProjectRepository
    {
        public ProjectRepository(ApplicationDbContext context) : base(context)
        {
        }

        // Перевизначаємо GetById, щоб підтягнути список завдань проекту
        public override async Task<Project?> GetByIdAsync(int id)
        {
            return await _dbSet
                .Include(p => p.Tasks)
                .Include(p => p.Manager)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<List<Project>> GetActiveProjectsAsync()
        {
            return await _dbSet
                .Where(p => p.Status == ProjectStatus.Active)
                .ToListAsync();
        }
    }
}