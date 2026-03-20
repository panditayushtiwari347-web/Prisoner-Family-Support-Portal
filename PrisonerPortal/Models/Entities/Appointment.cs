namespace PrisonerPortal.Models.Entities
{
    public class Appointment
    {
        public int AppointmentId { get; set; }
        public required string AppointmentCode { get; set; }
        
        public required string UserId { get; set; }
        public ApplicationUser? User { get; set; }
        
        public int PrisonerId { get; set; }
        public Prisoner? Prisoner { get; set; }
        
        public int SlotId { get; set; }
        public VisitSlot? Slot { get; set; }
        
        public required string Status { get; set; } // Pending/Approved/Rejected/Cancelled/Completed
        public required string Purpose { get; set; }
        public int NumVisitors { get; set; }
        public required string VisitorNames { get; set; } // JSON array
        
        public string? AdminNotes { get; set; }
        public string? RejectionReason { get; set; }
        
        public DateTime BookedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
