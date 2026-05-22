using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Security.Claims;
using System.Text.Json;
using TaskApi.Models;

namespace TaskApi.Data
{
    public class ApplicationDbContext : DbContext
    {
        private readonly IHttpContextAccessor? _httpContextAccessor;

        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options,
            IHttpContextAccessor? httpContextAccessor = null)
            : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public DbSet<TaskItem> Tasks { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<Executor> Executors { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<TaskHistory> TaskHistories { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }

        // Таблиця аудиту змін
        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            foreach (var entity in modelBuilder.Model.GetEntityTypes())
            {
                var tableName = entity.GetTableName();

                if (!string.IsNullOrEmpty(tableName))
                {
                    entity.SetTableName(tableName.ToLowerInvariant());
                }
            }

            modelBuilder.Entity<Comment>()
                .HasOne(c => c.TaskItem)
                .WithMany(t => t.Comments)
                .HasForeignKey(c => c.TaskItemId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TaskHistory>(entity =>
            {
                entity.ToTable("TaskHistories");
                entity.HasKey(h => h.Id);
                entity.HasOne(h => h.Task)
                    .WithMany()
                    .HasForeignKey(h => h.TaskItemId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users");
                entity.HasKey(u => u.Id);
                entity.Property(u => u.Email).IsRequired().HasMaxLength(255);
                entity.Property(u => u.PasswordHash).IsRequired().HasMaxLength(255);
                entity.Property(u => u.Role).IsRequired().HasMaxLength(50).HasDefaultValue("User");
                entity.Property(u => u.CreatedAt).IsRequired();
                entity.HasIndex(u => u.Email).IsUnique();
            });

            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.ToTable("RefreshTokens");
                entity.HasKey(r => r.Id);

                entity.Property(r => r.Token)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.HasIndex(r => r.Token).IsUnique();
                entity.HasIndex(r => r.UserId);

                entity.HasOne(r => r.User)
                    .WithMany()
                    .HasForeignKey(r => r.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.ToTable("AuditLogs");
                entity.HasKey(a => a.Id);

                entity.Property(a => a.EntityName).IsRequired().HasMaxLength(100);
                entity.Property(a => a.Action).IsRequired().HasMaxLength(50);
                entity.Property(a => a.EntityId).HasMaxLength(100);
                entity.Property(a => a.UserId).HasMaxLength(100);
                entity.Property(a => a.OldValues).HasColumnType("longtext");
                entity.Property(a => a.NewValues).HasColumnType("longtext");
                entity.Property(a => a.CreatedAt).IsRequired();
            });
        }



        public override async Task<int> SaveChangesAsync(
    CancellationToken cancellationToken = default)
        {
            var pendingAuditEntries = CreateAuditLogs();

            var result = await base.SaveChangesAsync(cancellationToken);

            if (pendingAuditEntries.Any())
            {
                foreach (var pendingAudit in pendingAuditEntries)
                {
                    // Після SaveChanges EF вже має реальний ID для Added-сутностей
                    if (pendingAudit.Entry.State != EntityState.Detached)
                    {
                        pendingAudit.AuditLog.EntityId = GetPrimaryKeyValue(pendingAudit.Entry);
                    }
                }

                AuditLogs.AddRange(pendingAuditEntries.Select(a => a.AuditLog));
                await base.SaveChangesAsync(cancellationToken);
            }

            return result;
        }

        private List<PendingAuditEntry> CreateAuditLogs()
        {
            ChangeTracker.DetectChanges();

            var auditEntries = new List<PendingAuditEntry>();
            var userId = GetCurrentUserId();

            var entries = ChangeTracker.Entries()
                .Where(e =>
                    e.Entity is not AuditLog &&
                    e.State != EntityState.Detached &&
                    e.State != EntityState.Unchanged)
                .ToList();

            foreach (var entry in entries)
            {
                var oldValues = new Dictionary<string, object?>();
                var newValues = new Dictionary<string, object?>();

                foreach (var property in entry.Properties)
                {
                    var propertyName = property.Metadata.Name;

                    if (property.Metadata.IsPrimaryKey())
                        continue;

                    // Не записуємо чутливі дані в аудит
                    if (IsSensitiveProperty(propertyName))
                        continue;

                    if (entry.State == EntityState.Added)
                    {
                        newValues[propertyName] = property.CurrentValue;
                    }
                    else if (entry.State == EntityState.Deleted)
                    {
                        oldValues[propertyName] = property.OriginalValue;
                    }
                    else if (entry.State == EntityState.Modified && property.IsModified)
                    {
                        oldValues[propertyName] = property.OriginalValue;
                        newValues[propertyName] = property.CurrentValue;
                    }
                }

                var auditLog = new AuditLog
                {
                    EntityName = entry.Entity.GetType().Name,
                    Action = entry.State.ToString(),

                    // Для Added ID буде оновлено після base.SaveChangesAsync
                    EntityId = entry.State == EntityState.Added
                        ? null
                        : GetPrimaryKeyValue(entry),

                    UserId = userId,
                    OldValues = oldValues.Any()
                        ? JsonSerializer.Serialize(oldValues)
                        : null,
                    NewValues = newValues.Any()
                        ? JsonSerializer.Serialize(newValues)
                        : null,
                    CreatedAt = DateTime.UtcNow
                };

                auditEntries.Add(new PendingAuditEntry
                {
                    Entry = entry,
                    AuditLog = auditLog
                });
            }

            return auditEntries;
        }

        private static bool IsSensitiveProperty(string propertyName)
        {
            string[] sensitiveNames =
            {
        "Password",
        "PasswordHash",
        "ConfirmPassword",
        "Token",
        "RefreshToken",
        "AccessToken",
        "ApiKey",
        "Secret"
    };

            return sensitiveNames.Any(name =>
                propertyName.Contains(name, StringComparison.OrdinalIgnoreCase));
        }

        private string GetCurrentUserId()
        {
            return _httpContextAccessor?.HttpContext?.User?
                .FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? _httpContextAccessor?.HttpContext?.User?
                .FindFirst("sub")?.Value
                ?? "System";
        }

        private static string? GetPrimaryKeyValue(EntityEntry entry)
        {
            var key = entry.Metadata.FindPrimaryKey();

            if (key == null)
                return null;

            var values = key.Properties
                .Select(p => entry.Property(p.Name).CurrentValue?.ToString())
                .Where(v => !string.IsNullOrWhiteSpace(v));

            return string.Join(",", values);
        }

        private class PendingAuditEntry
        {
            public EntityEntry Entry { get; set; } = null!;
            public AuditLog AuditLog { get; set; } = null!;
        }
    }
}