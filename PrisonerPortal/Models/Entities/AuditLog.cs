namespace PrisonerPortal.Models.Entities
{
    public class AuditLog
    {
        public int LogId { get; set; }
        
        public string? UserId { get; set; }
        public ApplicationUser? User { get; set; }
        
        public required string Action { get; set; } // Create/Update/Delete/Login
        public required string EntityType { get; set; }
        public required string EntityId { get; set; }
        public string? OldValues { get; set; } // JSON
        public string? NewValues { get; set; } // JSON
        public string? IPAddress { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
