using System.ComponentModel.DataAnnotations;

namespace ElectionShield.Models
{
    public class SimpleNepalCompareViewModel
    {
        [Required(ErrorMessage = "Please select the first manifesto file")]
        [Display(Name = "First Manifesto File")]
        public IFormFile FirstManifestoFile { get; set; }

        [Required(ErrorMessage = "Please select the second manifesto file")]
        [Display(Name = "Second Manifesto File")]
        public IFormFile SecondManifestoFile { get; set; }

        public SimpleComparisonResult Result { get; set; }
    }

    public class SimpleComparisonResult
    {
        public bool Success { get; set; }
        public string Error { get; set; }
        public double SimilarityPercentage { get; set; }
        public int TotalPromisesFound { get; set; }
        public int SimilarPromises { get; set; }
        public int DifferentPromises { get; set; }
        public List<string> ComparisonPoints { get; set; } = new List<string>();
    }
}