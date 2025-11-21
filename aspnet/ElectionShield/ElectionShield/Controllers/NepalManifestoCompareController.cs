// Controllers/NepalManifestoCompareController.cs
using Microsoft.AspNetCore.Mvc;
using ElectionShield.Models;
using System.Text;
using System.Text.RegularExpressions;

namespace ElectionShield.Controllers
{
    public class NepalManifestoCompareController : Controller
    {
        private readonly ILogger<NepalManifestoCompareController> _logger;

        public NepalManifestoCompareController(ILogger<NepalManifestoCompareController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View(new SimpleNepalCompareViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Compare(SimpleNepalCompareViewModel model)
        {
            //if (!ModelState.IsValid)
            //{
            //    return View("Index", model);
            //}

            try
            {
                // Extract text from both files
                var text1 = await ExtractTextFromFile(model.FirstManifestoFile);
                var text2 = await ExtractTextFromFile(model.SecondManifestoFile);

                if (string.IsNullOrWhiteSpace(text1) || string.IsNullOrWhiteSpace(text2))
                {
                    model.Result = new SimpleComparisonResult
                    {
                        Success = false,
                        Error = "Could not extract text from the uploaded files. Please ensure they contain readable text."
                    };
                    return View("Index", model);
                }

                // Compare the texts
                var result = CompareManifestos(text1, text2);
                model.Result = result;

                return View("Index", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error comparing manifestos");
                model.Result = new SimpleComparisonResult
                {
                    Success = false,
                    Error = "An error occurred while comparing the manifestos. Please try again."
                };
                return View("Index", model);
            }
        }

        private async Task<string> ExtractTextFromFile(IFormFile file)
        {
            try
            {
                using var stream = new StreamReader(file.OpenReadStream());
                return await stream.ReadToEndAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting text from file");
                return string.Empty;
            }
        }

        private SimpleComparisonResult CompareManifestos(string text1, string text2)
        {
            var result = new SimpleComparisonResult { Success = true };

            try
            {
                var promises1 = ExtractPromises(text1);
                var promises2 = ExtractPromises(text2);

                result.TotalPromisesFound = Math.Max(promises1.Count, promises2.Count);

                var similarCount = 0;
                var comparisonPoints = new List<string>();

                foreach (var promise1 in promises1.Take(20)) 
                {
                    var bestMatch = FindBestMatch(promise1, promises2);
                    if (bestMatch.similarity > 0.6) 
                    {
                        similarCount++;
                        comparisonPoints.Add($"✓ Similar: \"{Truncate(promise1, 50)}\"");
                    }
                    else if (!string.IsNullOrEmpty(bestMatch.match))
                    {
                        comparisonPoints.Add($"○ Different: \"{Truncate(promise1, 50)}\" vs \"{Truncate(bestMatch.match, 50)}\"");
                    }
                }

                result.SimilarPromises = similarCount;
                result.DifferentPromises = result.TotalPromisesFound - similarCount;
                result.SimilarityPercentage = result.TotalPromisesFound > 0 ?
                    (double)similarCount / result.TotalPromisesFound * 100 : 0;
                result.ComparisonPoints = comparisonPoints.Take(10).ToList();

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in comparison logic");
                return new SimpleComparisonResult { Success = false, Error = "Comparison failed" };
            }
        }

        private List<string> ExtractPromises(string text)
        {
            var sentences = Regex.Split(text, @"(?<=[।.!?])\s+")
                                .Where(s => !string.IsNullOrWhiteSpace(s))
                                .Where(s => s.Length > 10)
                                .Select(s => s.Trim())
                                .ToList();

            var nepaliPromiseKeywords = new[] {
                "गर्ने", "बनाउने", "सुधार्ने", "बढाउने", "घटाउने", "सुरक्षित", "निःशुल्क",
                "योजना", "कार्यक्रम", "विकास", "सुबिधा", "अधिकार", "सहयोग", "प्रवद्र्धन"
            };

            var promises = sentences.Where(s =>
                nepaliPromiseKeywords.Any(keyword => s.Contains(keyword)) ||
                s.Length > 20 
            ).ToList();

            return promises.Any() ? promises : sentences.Take(10).ToList(); 
        }

        private (string match, double similarity) FindBestMatch(string promise, List<string> promises)
        {
            if (!promises.Any()) return (string.Empty, 0);

            var bestMatch = string.Empty;
            var bestSimilarity = 0.0;

            foreach (var target in promises)
            {
                var similarity = CalculateSimilarity(promise, target);
                if (similarity > bestSimilarity)
                {
                    bestSimilarity = similarity;
                    bestMatch = target;
                }
            }

            return (bestMatch, bestSimilarity);
        }

        private double CalculateSimilarity(string text1, string text2)
        {
            // Simple word-based similarity
            var words1 = text1.Split(' ', '।', ',', ';', ':').Where(w => w.Length > 1).ToArray();
            var words2 = text2.Split(' ', '।', ',', ';', ':').Where(w => w.Length > 1).ToArray();

            if (words1.Length == 0 || words2.Length == 0) return 0;

            var commonWords = words1.Intersect(words2).Count();
            var totalWords = words1.Union(words2).Count();

            return totalWords > 0 ? (double)commonWords / totalWords : 0;
        }

        private string Truncate(string text, int maxLength)
        {
            return text.Length <= maxLength ? text : text.Substring(0, maxLength) + "...";
        }
    }
}