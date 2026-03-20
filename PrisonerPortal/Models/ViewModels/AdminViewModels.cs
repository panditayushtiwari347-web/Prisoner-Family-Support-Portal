using PrisonerPortal.Models.Entities;

namespace PrisonerPortal.Models.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalFamilies { get; set; }
        public int TotalPrisoners { get; set; }
        public int TodaysVisits { get; set; }
        public int OpenRequests { get; set; }
        public List<Appointment> PendingAppointments { get; set; } = new();
        public List<SupportRequest> RecentRequests { get; set; } = new();
        public List<FamilyPrisonerLink> PendingLinks { get; set; } = new();
        public List<ApplicationUser> RecentUsers { get; set; } = new();
    }

    public class AdminResponseViewModel
    {
        public int RequestId { get; set; }
        public required string Status { get; set; }
        public string? Response { get; set; }
    }

    public class VisitSlotViewModel
    {
        public int SlotId { get; set; }
        public DateTime SlotDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int MaxCapacity { get; set; }
    }
}
