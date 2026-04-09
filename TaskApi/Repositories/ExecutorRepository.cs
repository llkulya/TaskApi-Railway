using Microsoft.EntityFrameworkCore;
using TaskApi.Data;
using TaskApi.Models;

namespace TaskApi.Repositories
{
    public class ExecutorRepository : BaseRepository<Executor>, IExecutorRepository
    {
        public ExecutorRepository(ApplicationDbContext context) : base(context)
        {
        }

        public override async Task<Executor?> GetByIdAsync(int id)
        {
            return await _dbSet
                .Include(e => e.AssignedTasks)
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task<Executor?> GetByEmailAsync(string email)
        {
            return await _dbSet
                .FirstOrDefaultAsync(e => e.Email == email);
        }
    }
}