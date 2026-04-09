using Microsoft.EntityFrameworkCore;
using TaskApi.Data;
using TaskApi.Models;

namespace TaskApi.Repositories
{
    // Тепер ми наслідуємося від BaseRepository, щоб не писати дублюючий код CRUD
    public class ProjectManagerRepository : BaseRepository<ProjectManager>, IProjectManagerRepository
    {
        public ProjectManagerRepository(ApplicationDbContext context) : base(context)
        {
        }

        // Ми можемо перевизначити GetById, щоб підтягнути проєкти, якими керує цей менеджер
        public override async Task<ProjectManager?> GetByIdAsync(int id)
        {
            return await _dbSet
                .Include(m => m.ManagedProjects) // Додаємо зв'язок (Eager Loading)
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        // Всі інші методи (Add, Delete, GetAll) автоматично підтягнуться з BaseRepository
    }
}