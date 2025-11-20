using Microsoft.AspNetCore.Identity;

namespace ElectionShield.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FullName { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }
        public bool IsActive { get; set; } = true;
        public string? TimeZone { get; set; }
        public string? Language { get; set; } = "en";

        // Navigation properties
        public virtual ICollection<AdminVerification> Verifications { get; set; } = new List<AdminVerification>();
    }
}