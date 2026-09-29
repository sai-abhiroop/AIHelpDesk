using AIHelpDesk.Infrastructure.Identity;
using AIHelpDesk.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace AIHelpDesk.Tests.Services
{
    public class UserServiceTests
    {
        private static async Task<(SqliteConnection Connection, ApplicationDbContext Context, UserManager<ApplicationUser> UserManager, RoleManager<IdentityRole<Guid>> RoleManager, UserService Service)> CreateTestContextAsync()
        {
            var identityOptions = new Mock<IOptions<IdentityOptions>>();
            var serviceProvider = new Mock<IServiceProvider>();

            var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
            var context = new ApplicationDbContext(options);
            await context.Database.EnsureCreatedAsync();

            var userStore = new UserStore<ApplicationUser, IdentityRole<Guid>, ApplicationDbContext, Guid>(context);
            var userManager = new UserManager<ApplicationUser>(userStore, identityOptions.Object, new PasswordHasher<ApplicationUser>(), Array.Empty<IUserValidator<ApplicationUser>>(), Array.Empty<IPasswordValidator<ApplicationUser>>(), new UpperInvariantLookupNormalizer(), new IdentityErrorDescriber(), serviceProvider.Object, new Mock<ILogger<UserManager<ApplicationUser>>>().Object);

            var roleStore = new RoleStore<IdentityRole<Guid>, ApplicationDbContext, Guid>(context);
            var roleManager = new RoleManager<IdentityRole<Guid>>(roleStore, Array.Empty<IRoleValidator<IdentityRole<Guid>>>(), new UpperInvariantLookupNormalizer(), new IdentityErrorDescriber(), new Mock<ILogger<RoleManager<IdentityRole<Guid>>>>().Object);

            var service = new UserService(userManager);

            return (connection, context, userManager, roleManager, service);
        }

        [Fact]
        public async Task IsActiveSupportAgentAsync_ShouldReturnTrue_WhenUserIsActiveSupportAgent()
        {
            var (connection, context, userManager, roleManager, service) = await CreateTestContextAsync();

            await roleManager.CreateAsync(new IdentityRole<Guid>("SupportAgent"));

            var user = new ApplicationUser { UserName = "agent@example.com", Email = "agent@example.com", IsActive = true };
            await userManager.CreateAsync(user, "Password123!");
            await userManager.AddToRoleAsync(user, "SupportAgent");

            var result = await service.IsActiveSupportAgentAsync(user.Id);

            Assert.True(result);

            await context.DisposeAsync();
            await connection.DisposeAsync();
        }

        [Fact]
        public async Task IsActiveSupportAgentAsync_ShouldReturnFalse_WhenUserDoesNotExist()
        {
            var (connection, context, _, _, service) = await CreateTestContextAsync();

            var result = await service.IsActiveSupportAgentAsync(Guid.NewGuid());

            Assert.False(result);

            await context.DisposeAsync();
            await connection.DisposeAsync();
        }

        [Fact]
        public async Task IsActiveSupportAgentAsync_ShouldReturnFalse_WhenUserIsInactive()
        {
            var (connection, context, userManager, _, service) = await CreateTestContextAsync();

            var user = new ApplicationUser { UserName = "agent@example.com", Email = "agent@example.com", IsActive = false };
            await userManager.CreateAsync(user, "Password123!");

            var result = await service.IsActiveSupportAgentAsync(user.Id);

            Assert.False(result);

            await context.DisposeAsync();
            await connection.DisposeAsync();
        }

        [Fact]
        public async Task IsActiveSupportAgentAsync_ShouldReturnFalse_WhenUserIsNotSupportAgent()
        {
            var (connection, context, userManager, _, service) = await CreateTestContextAsync();

            var user = new ApplicationUser { UserName = "customer@example.com", Email = "customer@example.com", IsActive = true };
            await userManager.CreateAsync(user, "Password123!");

            var result = await service.IsActiveSupportAgentAsync(user.Id);

            Assert.False(result);

            await context.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}
