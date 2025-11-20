using Microsoft.AspNetCore.Identity;
using ElectionShield.Models;

namespace ElectionShield.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            // Create roles
            string[] roleNames = { "Admin", "User", "Moderator" };

            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // Create admin user
            var adminEmail = "admin@electionshield.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "Bishwas Paudel",
                    EmailConfirmed = true,
                    PhoneNumber = "9866317885",
                    PhoneNumberConfirmed = true
                };

                var createPowerUser = await userManager.CreateAsync(adminUser, "Admin123!");
                if (createPowerUser.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                    await userManager.AddToRoleAsync(adminUser, "Moderator");
                }
            }
            else
            {
                // Ensure admin has correct roles
                var userRoles = await userManager.GetRolesAsync(adminUser);
                if (!userRoles.Contains("Admin"))
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }

            // Create moderator user
            var moderatorEmail = "moderator@electionshield.com";
            var moderatorUser = await userManager.FindByEmailAsync(moderatorEmail);
            if (moderatorUser == null)
            {
                moderatorUser = new ApplicationUser
                {
                    UserName = moderatorEmail,
                    Email = moderatorEmail,
                    FullName = "Content Moderator",
                    EmailConfirmed = true
                };

                var createModerator = await userManager.CreateAsync(moderatorUser, "Votekinyo!");
                if (createModerator.Succeeded)
                {
                    await userManager.AddToRoleAsync(moderatorUser, "Moderator");
                }
            }
        }
    }
}