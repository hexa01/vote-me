using System.ComponentModel.DataAnnotations;

namespace ElectionShield.Models
{
    public class Manifesto
    {
        [Key]
        public int Id { get; set; }
        public string? Fulfilled { get; set; }

        public string? Unfulfilled {get; set;}
        public string? UserId {get; set;}
        public string? PoliticalName {get; set;}
    }
}