using Microsoft.EntityFrameworkCore;
using TaskApi.Models;

namespace TaskApi.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Твої таблиці в MySQL
        public DbSet<TaskItem> Tasks { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<Executor> Executors { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Тут ми пізніше налаштуємо зв'язки, 
            // якщо автоматика не розбереться з ProjectId та ExecutorId
        }
    }
}