namespace PrisonerPortal.Models.Entities
{
    public class FamilyPrisonerLink
    {
        public int LinkId { get; set; }
        public required string UserId { get; set; }
        public ApplicationUser? User { get; set; }

        public int PrisonerId { get; set; }
        public Prisoner? Prisoner { get; set; }

        public required string Relationship { get; set; } // Spouse/Parent/Child/Sibling/Other
        public bool IsVerified { get; set; } = false;
        
        public string? VerifiedByAdminId { get; set; }
        public ApplicationUser? VerifiedByAdmin { get; set; }

        public DateTime LinkedAt { get; set; } = DateTime.UtcNow;
    }
}
