namespace PrisonerPortal.Models.Entities
{
    public class VisitSlot
    {
        public int SlotId { get; set; }
        public DateTime SlotDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int MaxCapacity { get; set; } = 10;
        public int CurrentBookings { get; set; } = 0;
        public bool IsActive { get; set; } = true;
        
        public required string CreatedByAdminId { get; set; }
        public ApplicationUser? CreatedByAdmin { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    }
}
