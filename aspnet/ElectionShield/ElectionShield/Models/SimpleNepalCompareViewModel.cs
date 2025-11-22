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
        public CompareApiResponse ApiRaw { get; set; }
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


    public class CompareApiResponse
    {
        public double overall_score { get; set; }
        public List<TopPair> top_pairs { get; set; }
        public string summary_a { get; set; }
        public string summary_b { get; set; }
        public List<string> unique_a { get; set; }
        public List<string> unique_b { get; set; }
    }

    public class TopPair
    {
        public double score { get; set; }
        public string para_a { get; set; }
        public string para_b { get; set; }
    }

}