using AIHelpDesk.Domain.Entities;
using AIHelpDesk.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AIHelpDesk.Infrastructure.Persistence
{
    public class DbInitializer
    {
        public static async Task SeedAsync(ApplicationDbContext dbContext, RoleManager<IdentityRole<Guid>> roleManager, UserManager<ApplicationUser> userManager, IConfiguration configuration)
        {
            // Seed roles
            string[] roles =["Customer","SupportAgent","Admin"];

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole<Guid>(role));
                }
            }

            //seed Categeories
            if (!await dbContext.Categories.AnyAsync())
            {
                var categories = new[]
                {
                    new Category("Account & Login", "Problems related to accounts, passwords, and login."),

                    new Category("Billing", "Questions and problems related to billing and payments."),

                    new Category("Technical Issue", "Technical problems with the product or service."),

                    new Category("Booking", "Questions and problems related to bookings."),

                    new Category("General Inquiry", "General questions and information requests.")
                };

                await dbContext.Categories.AddRangeAsync(categories);
                await dbContext.SaveChangesAsync();
            }

            //seed users
            var seedUsers = new[]
            {
                new
                {
                    Email = "customer@aihelpdesk.local",
                    FirstName = "Test",
                    LastName = "Customer",
                    Role = "Customer"
                },
                new
                {
                    Email = "agent@aihelpdesk.local",
                    FirstName = "Test",
                    LastName = "Agent",
                    Role = "SupportAgent"
                },
                new
                {
                    Email = "admin@aihelpdesk.local",
                    FirstName = "Test",
                    LastName = "Admin",
                    Role = "Admin"
                }
            };

            var password = configuration["SeedUsers:Password"] ?? throw new InvalidOperationException("SeedUsers:Password is not configured.");

            foreach (var seedUser in seedUsers)
            {
                var existingUser =await userManager.FindByEmailAsync(seedUser.Email);

                if (existingUser != null)
                    continue;

                var user = new ApplicationUser
                {
                    FirstName = seedUser.FirstName,
                    LastName = seedUser.LastName,
                    Email = seedUser.Email,
                    UserName = seedUser.Email,
                    EmailConfirmed = true,
                    IsActive = true
                };

                var result = await userManager.CreateAsync(user,password);

                if (!result.Succeeded)
                {
                    var errors = string.Join(  ", ", result.Errors.Select(e => e.Description));

                    throw new InvalidOperationException($"Failed to create seed user {seedUser.Email}: {errors}");
                }

                var roleResult = await userManager.AddToRoleAsync(user, seedUser.Role);

                if (!roleResult.Succeeded)
                {
                    var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));

                    throw new InvalidOperationException($"Failed to assign role {seedUser.Role} to {seedUser.Email}: {errors}");
                }
            }
        }
    }
}
