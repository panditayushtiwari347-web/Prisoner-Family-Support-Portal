namespace PrisonerPortal.Models.Entities
{
    public class SupportRequest
    {
        public int RequestId { get; set; }
        public required string RequestCode { get; set; }
        
        public required string UserId { get; set; }
        public ApplicationUser? User { get; set; }
        
        public int? PrisonerId { get; set; }
        public Prisoner? Prisoner { get; set; }
        
        public required string Category { get; set; } // Legal Aid/Medical Concern/Welfare/Transfer Request/Other
        public required string Subject { get; set; }
        public required string Description { get; set; }
        public required string Priority { get; set; } // Low/Medium/High/Urgent
        public required string Status { get; set; } // Open/InProgress/Resolved/Closed
        
        public string? AdminResponse { get; set; }
        public string? AssignedAdminId { get; set; }
        public ApplicationUser? AssignedAdmin { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ResolvedAt { get; set; }
    }
}
