using Microsoft.AspNetCore.Identity;

namespace PrisonerPortal.Models.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public required string FullName { get; set; }
        public string? Address { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
        public string Role { get; set; } = "Family"; // Admin | Family

        // Navigation properties
        public ICollection<FamilyPrisonerLink> FamilyLinks { get; set; } = new List<FamilyPrisonerLink>();
        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
        public ICollection<SupportRequest> SupportRequests { get; set; } = new List<SupportRequest>();
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}
