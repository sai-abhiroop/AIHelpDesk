using AIHelpDesk.Application.Dtos.Auth;
using AIHelpDesk.Infrastructure.Authentication;
using AIHelpDesk.Infrastructure.Identity;
using AIHelpDesk.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace AIHelpDesk.Tests.Services
{
    public class AuthServiceTests
    {
        private static async Task<(SqliteConnection Connection, ApplicationDbContext Context, UserManager<ApplicationUser> UserManager, RoleManager<IdentityRole<Guid>> RoleManager, Mock<AIHelpDesk.Application.Interfaces.ITokenService> TokenServiceMock, AIHelpDesk.Infrastructure.Identity.AuthService Service)> CreateTestContextAsync()
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

            var jwtSettings = Options.Create(new JwtSettings
            {
                SecretKey = "test-secret-key-that-is-long-enough-for-testing",
                Issuer = "AIHelpDesk.Tests",
                Audience = "AIHelpDesk.Tests",
                AccessTokenExpirationMinutes = 30,
                RefreshTokenExpirationMinutes = 10080
            });

            var tokenServiceMock = new Mock<AIHelpDesk.Application.Interfaces.ITokenService>();
            var loggerMock = new Mock<ILogger<AIHelpDesk.Infrastructure.Identity.AuthService>>();

            var service = new AIHelpDesk.Infrastructure.Identity.AuthService(userManager, roleManager, jwtSettings, tokenServiceMock.Object, loggerMock.Object);

            return (connection, context, userManager, roleManager, tokenServiceMock, service);
        }

        [Fact]
        public async Task RegisterAsync_ShouldCreateCustomerSuccessfully()
        {
            // Arrange
            var test = await CreateTestContextAsync();
            await using var connection = test.Connection;
            await using var context = test.Context;
            await test.RoleManager.CreateAsync(new IdentityRole<Guid>("Customer"));

            var dto = new RegisterationRequestDto { FirstName = "John", LastName = "Doe", Email = "john@example.com", Password = "Password123!" };

            // Act
            var result = await test.Service.RegisterAsync(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("John", result.FirstName);
            Assert.Equal("Doe", result.LastName);
            Assert.Equal("john@example.com", result.Email);
            Assert.Equal("Customer", result.Role);

            var user = await test.UserManager.FindByEmailAsync("john@example.com");

            Assert.NotNull(user);
            Assert.Equal("John", user.FirstName);
            Assert.Equal("Doe", user.LastName);
            Assert.True(user.IsActive);

            var roles = await test.UserManager.GetRolesAsync(user);
            Assert.Contains("Customer", roles);
        }

        [Fact]
        public async Task RegisterAsync_WhenEmailAlreadyExists_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var test = await CreateTestContextAsync();
            await using var connection = test.Connection;
            await using var context = test.Context;
            await test.RoleManager.CreateAsync(new IdentityRole<Guid>("Customer"));

            var existingUser = new ApplicationUser { FirstName = "Existing", LastName = "User", Email = "existing@example.com", UserName = "existing@example.com", EmailConfirmed = true, IsActive = true };

            await test.UserManager.CreateAsync(existingUser, "Password123!");

            var dto = new RegisterationRequestDto { FirstName = "John", LastName = "Doe", Email = "existing@example.com", Password = "Password123!" };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => test.Service.RegisterAsync(dto));

            Assert.Equal("A user with email 'existing@example.com' already exists.", exception.Message);

            var users = await context.Users.ToListAsync();
            Assert.Single(users);
        }

        [Fact]
        public async Task IsEmailExistsAsync_WhenEmailExists_ShouldReturnTrue()
        {
            // Arrange
            var test = await CreateTestContextAsync();
            await using var connection = test.Connection;
            await using var context = test.Context;

            var user = new ApplicationUser { FirstName = "John", LastName = "Doe", Email = "john@example.com", UserName = "john@example.com" };

            await test.UserManager.CreateAsync(user, "Password123!");

            // Act
            var result = await test.Service.IsEmailExistsAsync("john@example.com");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task IsEmailExistsAsync_WhenEmailDoesNotExist_ShouldReturnFalse()
        {
            // Arrange
            var test = await CreateTestContextAsync();
            await using var connection = test.Connection;
            await using var context = test.Context;

            // Act
            var result = await test.Service.IsEmailExistsAsync("missing@example.com");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task LoginAsync_WithValidCredentials_ShouldReturnToken()
        {
            // Arrange
            var test = await CreateTestContextAsync();
            await using var connection = test.Connection;
            await using var context = test.Context;

            await test.RoleManager.CreateAsync(new IdentityRole<Guid>("Customer"));

            var user = new ApplicationUser { FirstName = "John", LastName = "Doe", Email = "john@example.com", UserName = "john@example.com", EmailConfirmed = true, IsActive = true };

            await test.UserManager.CreateAsync(user, "Password123!");
            await test.UserManager.AddToRoleAsync(user, "Customer");

            var jwtTokenId = Guid.NewGuid().ToString();
            var accessToken = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(claims: new[] { new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), new Claim(JwtRegisteredClaimNames.Jti, jwtTokenId) }));

            test.TokenServiceMock.Setup(service => service.GenerateJwtTokenAsync(user.Id, user.Email!, It.IsAny<IEnumerable<string>>())).ReturnsAsync(accessToken);
            test.TokenServiceMock.Setup(service => service.GenerateRefreshTokenAsync()).ReturnsAsync("refresh-token");

            var dto = new LoginRequestDto { Email = "john@example.com", Password = "Password123!" };

            // Act
            var result = await test.Service.LoginAsync(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(accessToken, result.AccessToken);
            Assert.Equal("refresh-token", result.RefreshToken);
            test.TokenServiceMock.Verify(service => service.GenerateJwtTokenAsync(user.Id, user.Email!, It.Is<IEnumerable<string>>(roles => roles.Contains("Customer"))), Times.Once);
            test.TokenServiceMock.Verify(service => service.GenerateRefreshTokenAsync(), Times.Once);
        }

        [Fact]
        public async Task LoginAsync_WithInvalidPassword_ShouldReturnNull()
        {
            // Arrange
            var test = await CreateTestContextAsync();
            await using var connection = test.Connection;
            await using var context = test.Context;

            var user = new ApplicationUser { FirstName = "John", LastName = "Doe", Email = "john@example.com", UserName = "john@example.com", EmailConfirmed = true, IsActive = true };

            await test.UserManager.CreateAsync(user, "Password123!");

            var dto = new LoginRequestDto { Email = "john@example.com", Password = "WrongPassword123!" };

            // Act
            var result = await test.Service.LoginAsync(dto);

            // Assert
            Assert.Null(result);
            test.TokenServiceMock.Verify(service => service.GenerateJwtTokenAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>()), Times.Never);
            test.TokenServiceMock.Verify(service => service.GenerateRefreshTokenAsync(), Times.Never);
        }

        [Fact]
        public async Task LoginAsync_WhenEmailDoesNotExist_ShouldReturnNull()
        {
            // Arrange
            var test = await CreateTestContextAsync();
            await using var connection = test.Connection;
            await using var context = test.Context;

            var dto = new LoginRequestDto { Email = "missing@example.com", Password = "Password123!" };

            // Act
            var result = await test.Service.LoginAsync(dto);

            // Assert
            Assert.Null(result);
            test.TokenServiceMock.Verify(service => service.GenerateJwtTokenAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>()), Times.Never);
            test.TokenServiceMock.Verify(service => service.GenerateRefreshTokenAsync(), Times.Never);
        }

        [Fact]
        public async Task LoginAsync_WhenUserIsInactive_ShouldReturnNull()
        {
            // Arrange
            var test = await CreateTestContextAsync();
            await using var connection = test.Connection;
            await using var context = test.Context;

            var user = new ApplicationUser { FirstName = "John", LastName = "Doe", Email = "john@example.com", UserName = "john@example.com", EmailConfirmed = true, IsActive = false };

            await test.UserManager.CreateAsync(user, "Password123!");

            var dto = new LoginRequestDto { Email = "john@example.com", Password = "Password123!" };

            // Act
            var result = await test.Service.LoginAsync(dto);

            // Assert
            Assert.Null(result);
            test.TokenServiceMock.Verify(service => service.GenerateJwtTokenAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>()), Times.Never);
            test.TokenServiceMock.Verify(service => service.GenerateRefreshTokenAsync(), Times.Never);
        }

        [Fact]
        public async Task RefreshAccessTokenAsync_WithValidRefreshToken_ShouldReturnNewTokens()
        {
            // Arrange
            var test = await CreateTestContextAsync();
            await using var connection = test.Connection;
            await using var context = test.Context;

            var user = new ApplicationUser { FirstName = "John", LastName = "Doe", Email = "john@example.com", UserName = "john@example.com", EmailConfirmed = true, IsActive = true };

            await test.UserManager.CreateAsync(user, "Password123!");
            await test.RoleManager.CreateAsync(new IdentityRole<Guid>("Customer"));
            await test.UserManager.AddToRoleAsync(user, "Customer");

            var jwtTokenId = Guid.NewGuid().ToString();
            var oldRefreshToken = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(claims: new[] { new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), new Claim(JwtRegisteredClaimNames.Jti, jwtTokenId) }));
            var newRefreshToken = "new-refresh-token";
            var tokenFamilyId = Guid.NewGuid().ToString();

            test.TokenServiceMock.Setup(service => service.ValidateRefreshTokenAsync(oldRefreshToken)).ReturnsAsync((true, user.Id.ToString(), tokenFamilyId, false));
            test.TokenServiceMock.Setup(service => service.RevokeRefreshTokenAsync(oldRefreshToken)).ReturnsAsync(true);

            var newJwtTokenId = Guid.NewGuid().ToString();
            var newAccessToken = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(claims: new[] { new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), new Claim(JwtRegisteredClaimNames.Jti, newJwtTokenId) }));

            test.TokenServiceMock.Setup(service => service.GenerateJwtTokenAsync(user.Id, user.Email!, It.IsAny<IEnumerable<string>>())).ReturnsAsync(newAccessToken);
            test.TokenServiceMock.Setup(service => service.GenerateRefreshTokenAsync()).ReturnsAsync(newRefreshToken);
            test.TokenServiceMock.Setup(service => service.SaveRefreshTokenAsync(user.Id, newJwtTokenId, newRefreshToken, It.IsAny<DateTime>(), tokenFamilyId)).Returns(Task.CompletedTask);

            var dto = new RefreshTokenRequestDto { RefreshToken = oldRefreshToken };

            // Act
            var result = await test.Service.RefreshAccessTokenAsync(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(newAccessToken, result.AccessToken);
            Assert.Equal(newRefreshToken, result.RefreshToken);
            test.TokenServiceMock.Verify(service => service.ValidateRefreshTokenAsync(oldRefreshToken), Times.Once);
            test.TokenServiceMock.Verify(service => service.RevokeRefreshTokenAsync(oldRefreshToken), Times.Once);
            test.TokenServiceMock.Verify(service => service.GenerateJwtTokenAsync(user.Id, user.Email!, It.IsAny<IEnumerable<string>>()), Times.Once);
            test.TokenServiceMock.Verify(service => service.GenerateRefreshTokenAsync(), Times.Once);
        }

        [Fact]
        public async Task RefreshAccessTokenAsync_WhenRefreshTokenIsInvalid_ShouldReturnNull()
        {
            // Arrange
            var test = await CreateTestContextAsync();
            await using var connection = test.Connection;
            await using var context = test.Context;

            var refreshToken = "invalid-refresh-token";

            test.TokenServiceMock.Setup(service => service.ValidateRefreshTokenAsync(refreshToken)).ReturnsAsync((false, null, null, false));

            var dto = new RefreshTokenRequestDto { RefreshToken = refreshToken };

            // Act
            var result = await test.Service.RefreshAccessTokenAsync(dto);

            // Assert
            Assert.Null(result);
            test.TokenServiceMock.Verify(service => service.RevokeRefreshTokenAsync(It.IsAny<string>()), Times.Never);
            test.TokenServiceMock.Verify(service => service.GenerateJwtTokenAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>()), Times.Never);
        }

        [Fact]
        public async Task RefreshAccessTokenAsync_WhenRefreshTokenIsReused_ShouldReturnNull()
        {
            // Arrange
            var test = await CreateTestContextAsync();
            await using var connection = test.Connection;
            await using var context = test.Context;

            var refreshToken = "reused-refresh-token";
            var userId = Guid.NewGuid();
            var tokenFamilyId = Guid.NewGuid().ToString();

            test.TokenServiceMock.Setup(service => service.ValidateRefreshTokenAsync(refreshToken)).ReturnsAsync((false, userId.ToString(), tokenFamilyId, true));

            var dto = new RefreshTokenRequestDto { RefreshToken = refreshToken };

            // Act
            var result = await test.Service.RefreshAccessTokenAsync(dto);

            // Assert
            Assert.Null(result);
            test.TokenServiceMock.Verify(service => service.GenerateJwtTokenAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>()), Times.Never);
            test.TokenServiceMock.Verify(service => service.GenerateRefreshTokenAsync(), Times.Never);
        }

        [Fact]
        public async Task RefreshAccessTokenAsync_WhenUserDoesNotExist_ShouldReturnNull()
        {
            // Arrange
            var test = await CreateTestContextAsync();
            await using var connection = test.Connection;
            await using var context = test.Context;

            var userId = Guid.NewGuid();
            var tokenFamilyId = Guid.NewGuid().ToString();
            var refreshToken = "valid-refresh-token";

            test.TokenServiceMock.Setup(service => service.ValidateRefreshTokenAsync(refreshToken)).ReturnsAsync((true, userId.ToString(), tokenFamilyId, false));

            var dto = new RefreshTokenRequestDto { RefreshToken = refreshToken };

            // Act
            var result = await test.Service.RefreshAccessTokenAsync(dto);

            // Assert
            Assert.Null(result);
            test.TokenServiceMock.Verify(service => service.RevokeRefreshTokenAsync(It.IsAny<string>()), Times.Never);
            test.TokenServiceMock.Verify(service => service.GenerateJwtTokenAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>()), Times.Never);
        }
    }
}