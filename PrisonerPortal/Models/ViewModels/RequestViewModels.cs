using System.ComponentModel.DataAnnotations;
using PrisonerPortal.Models.Entities;

namespace PrisonerPortal.Models.ViewModels
{
    public class SubmitRequestViewModel
    {
        [Display(Name = "Prisoner")]
        public int? PrisonerId { get; set; }

        [Required]
        public required string Category { get; set; }

        [Required]
        public required string Priority { get; set; }

        [Required]
        [StringLength(200)]
        public required string Subject { get; set; }

        [Required]
        [StringLength(2000, MinimumLength = 50, ErrorMessage = "Description must be between 50 and 2000 characters.")]
        public required string Description { get; set; }

        public IEnumerable<FamilyPrisonerLink> LinkedPrisoners { get; set; } = new List<FamilyPrisonerLink>();
    }

    // AdminResponseViewModel is defined in AdminViewModels.cs
}

