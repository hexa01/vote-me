using System.ComponentModel.DataAnnotations;

namespace ElectionShield.ViewModels
{
    public class CreateReportViewModel
    {
        [Required(ErrorMessage = "Title is required")]
        [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
        [Display(Name = "Incident Title")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        [StringLength(4000, ErrorMessage = "Description cannot exceed 4000 characters")]
        [Display(Name = "Detailed Description")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Location description is required")]
        [StringLength(500, ErrorMessage = "Location cannot exceed 500 characters")]
        [Display(Name = "Location Description")]
        public string Location { get; set; } = string.Empty;

        [Required(ErrorMessage = "Category is required")]
        [Display(Name = "Incident Category")]
        public string Category { get; set; } = "Other";

        [Required(ErrorMessage = "Latitude is required")]
        [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90")]
        public double Latitude { get; set; }

        [Required(ErrorMessage = "Longitude is required")]
        [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180")]
        public double Longitude { get; set; }

        [Display(Name = "Submit anonymously")]
        public bool IsAnonymous { get; set; } = true;

        [EmailAddress(ErrorMessage = "Invalid email address")]
        [Display(Name = "Email Address (optional)")]
        public string? ReporterEmail { get; set; }

        [Phone(ErrorMessage = "Invalid phone number")]
        [Display(Name = "Phone Number (optional)")]
        public string? ReporterPhone { get; set; }

        [Display(Name = "Upload Evidence")]
        public List<IFormFile>? MediaFiles { get; set; }

        [Display(Name = "I confirm that this report is accurate to the best of my knowledge")]
        public bool Confirmation { get; set; }

        public string? CreatedBy { get; set; }
    }
}