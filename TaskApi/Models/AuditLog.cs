namespace TaskApi.Models
{
    public class AuditLog
    {
        public int Id { get; set; }

        public string EntityName { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;

        public string? EntityId { get; set; }
        public string? UserId { get; set; }

        public string? OldValues { get; set; }
        public string? NewValues { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}