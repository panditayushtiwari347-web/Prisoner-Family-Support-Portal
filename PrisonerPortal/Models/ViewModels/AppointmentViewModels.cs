using System.ComponentModel.DataAnnotations;
using PrisonerPortal.Models.Entities;

namespace PrisonerPortal.Models.ViewModels
{
    public class BookVisitViewModel
    {
        [Required(ErrorMessage = "Please select a prisoner.")]
        [Display(Name = "Prisoner")]
        public int PrisonerId { get; set; }

        [Required(ErrorMessage = "Please select a slot.")]
        [Display(Name = "Available Slots")]
        public int SlotId { get; set; }

        [Required]
        public required string Purpose { get; set; }

        [Required]
        [Range(1, 4, ErrorMessage = "Number of visitors must be between 1 and 4.")]
        [Display(Name = "Number of Visitors")]
        public int NumVisitors { get; set; }

        [Required(ErrorMessage = "Please provide the names of all visitors.")]
        [Display(Name = "Visitor Names (comma separated)")]
        public required string VisitorNames { get; set; }

        [Display(Name = "Any additional notes?")]
        public string? Notes { get; set; }

        public IEnumerable<FamilyPrisonerLink> LinkedPrisoners { get; set; } = new List<FamilyPrisonerLink>();
        public IEnumerable<VisitSlot> AvailableSlots { get; set; } = new List<VisitSlot>();
    }

    // VisitSlotViewModel is defined in AdminViewModels.cs
}

