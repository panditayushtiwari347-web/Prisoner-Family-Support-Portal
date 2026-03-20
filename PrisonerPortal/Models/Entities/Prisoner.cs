namespace PrisonerPortal.Models.Entities
{
    public class Prisoner
    {
        public int PrisonerId { get; set; }
        public required string PrisonerCode { get; set; } // auto-generated
        public required string FullName { get; set; }
        public DateTime DateOfBirth { get; set; }
        public required string Gender { get; set; } // Male/Female/Other
        public required string CaseNumber { get; set; } // unique
        public required string CrimeCategory { get; set; }
        public DateTime SentenceStartDate { get; set; }
        public DateTime SentenceEndDate { get; set; }
        public required string CellBlock { get; set; }
        public required string CellNumber { get; set; }
        public required string Status { get; set; } // Active/Released/Transferred
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<FamilyPrisonerLink> FamilyLinks { get; set; } = new List<FamilyPrisonerLink>();
        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
        public ICollection<SupportRequest> SupportRequests { get; set; } = new List<SupportRequest>();
    }
}
