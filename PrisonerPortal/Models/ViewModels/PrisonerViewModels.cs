using System.ComponentModel.DataAnnotations;

namespace PrisonerPortal.Models.ViewModels
{
    public class PrisonerViewModel
    {
        public int PrisonerId { get; set; }
        public string? PrisonerCode { get; set; }

        [Required]
        [Display(Name = "Full Name")]
        public required string FullName { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime DateOfBirth { get; set; }

        [Required]
        public required string Gender { get; set; }

        [Required]
        [Display(Name = "Case Number")]
        public required string CaseNumber { get; set; }

        [Required]
        [Display(Name = "Crime Category")]
        public required string CrimeCategory { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Sentence Start Date")]
        public DateTime SentenceStartDate { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Sentence End Date")]
        public DateTime SentenceEndDate { get; set; }

        [Required]
        [Display(Name = "Cell Block")]
        public required string CellBlock { get; set; }

        [Required]
        [Display(Name = "Cell Number")]
        public required string CellNumber { get; set; }

        [Required]
        public required string Status { get; set; }
    }

    public class LinkRequestViewModel
    {
        public int PrisonerId { get; set; }
        
        [Required]
        public required string Relationship { get; set; }
    }
}
