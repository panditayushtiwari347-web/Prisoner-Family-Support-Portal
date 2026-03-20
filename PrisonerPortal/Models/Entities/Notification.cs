namespace PrisonerPortal.Models.Entities
{
    public class Notification
    {
        public int NotificationId { get; set; }
        
        public required string UserId { get; set; }
        public ApplicationUser? User { get; set; }
        
        public required string Title { get; set; }
        public required string Message { get; set; }
        public required string Type { get; set; } // BookingUpdate/RequestUpdate/SystemAlert/Welcome
        public bool IsRead { get; set; } = false;
        
        public int? RelatedEntityId { get; set; }
        public string? RelatedEntityType { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
