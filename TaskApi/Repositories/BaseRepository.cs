namespace TaskApi.Repositories
{
    public class BaseRepository<T> : IBaseRepository<T> where T : class
    {
        // Наше сховище в пам'яті
        protected readonly List<T> _entities = new List<T>();

        public async Task<List<T>> GetAllAsync() => _entities.ToList();

        public async Task<T?> GetByIdAsync(int id)
        {
            // Шукаємо властивість "Id" у будь-якого об'єкта T
            return _entities.FirstOrDefault(e =>
                (int)e.GetType().GetProperty("Id")?.GetValue(e)! == id);
        }

        public virtual async Task<T> AddAsync(T entity)
        {
            // Отримуємо властивість 'Id' через рефлексію
            var idProperty = typeof(T).GetProperty("Id");

            if (idProperty != null)
            {
                // Дивимося, які ID вже є в нашому списку _entities
                var existingIds = _entities
                    .Select(e => (int)idProperty.GetValue(e)!)
                    .ToList();

                // Визначаємо наступний ID (якщо список порожній — почнемо з 1)
                int nextId = existingIds.Any() ? existingIds.Max() + 1 : 1;

                // Встановлюємо новий ID для нашого об'єкта
                idProperty.SetValue(entity, nextId);
            }

            _entities.Add(entity);
            return await Task.FromResult(entity);
        }

        public async Task<T?> UpdateAsync(T entity)
        {
            var id = (int)entity.GetType().GetProperty("Id")?.GetValue(entity)!;
            var existing = await GetByIdAsync(id);
            if (existing != null)
            {
                _entities.Remove(existing);
                _entities.Add(entity);
            }
            return entity;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await GetByIdAsync(id);
            if (entity != null)
            {
                _entities.Remove(entity);
                return true;
            }
            return false;
        }
    }
}
