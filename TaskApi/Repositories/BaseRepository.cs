using Microsoft.EntityFrameworkCore;
using TaskApi.Data;

namespace TaskApi.Repositories
{
    /// <summary>
    /// Базовий репозиторій, що реалізує стандартні CRUD операції через Entity Framework Core.
    /// </summary>
    /// <typeparam name="T">Клас моделі (сутності)</typeparam>
    public class BaseRepository<T> : IBaseRepository<T> where T : class
    {
        protected readonly ApplicationDbContext _context;
        protected readonly DbSet<T> _dbSet;

        public BaseRepository(ApplicationDbContext context)
        {
            // Впровадження контексту бази даних
            _context = context;
            // Отримуємо набір даних для конкретного типу T
            _dbSet = context.Set<T>();
        }

        public virtual async Task<List<T>> GetAllAsync()
        {
            // Виконуємо асинхронний запит SELECT * FROM ...
            return await _dbSet.ToListAsync();
        }

        public virtual async Task<T?> GetByIdAsync(int id)
        {
            // Знаходимо запис за первинним ключем (Id)
            return await _dbSet.FindAsync(id);
        }

        public virtual async Task<T> AddAsync(T entity)
        {
            // Додаємо об'єкт у контекст
            await _dbSet.AddAsync(entity);
            // Фіксуємо зміни в MySQL (генерується INSERT)
            await _context.SaveChangesAsync();
            return entity;
        }

        public virtual async Task<T?> UpdateAsync(T entity)
        {
            // Позначаємо сутність як змінену
            _context.Entry(entity).State = EntityState.Modified;
            // Фіксуємо зміни в MySQL (генерується UPDATE)
            await _context.SaveChangesAsync();
            return entity;
        }

        public virtual async Task<bool> DeleteAsync(int id)
        {
            var entity = await GetByIdAsync(id);
            if (entity == null) return false;

            // Видаляємо сутність
            _dbSet.Remove(entity);
            // Фіксуємо зміни в MySQL (генерується DELETE)
            await _context.SaveChangesAsync();
            return true;
        }
    }
}