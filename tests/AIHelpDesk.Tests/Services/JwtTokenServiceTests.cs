using AIHelpDesk.Infrastructure.Authentication;
using AIHelpDesk.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace AIHelpDesk.Tests.Services
{
    public class JwtTokenServiceTests
    {
        private static async Task<(SqliteConnection Connection, ApplicationDbContext Context, JwtTokenService Service)> CreateTestContextAsync()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
            var context = new ApplicationDbContext(options);
            await context.Database.EnsureCreatedAsync();

            var jwtSettings = Options.Create(new JwtSettings
            {
                SecretKey = "test-secret-key-that-is-long-enough-for-testing",
                Issuer = "AIHelpDesk.Tests",
                Audience = "AIHelpDesk.Tests",
                AccessTokenExpirationMinutes = 30,
                RefreshTokenExpirationMinutes = 10080
            });

            var service = new JwtTokenService(jwtSettings, context);

            return (connection, context, service);
        }

        [Fact]
        public async Task GenerateJwtTokenAsync_ShouldGenerateValidJwtToken()
        {
            // Arrange
            var test = await CreateTestContextAsync();
            await using var connection = test.Connection;
            await using var context = test.Context;
            var userId = Guid.NewGuid();
            var email = "john@example.com";
            var roles = new[] { "Customer" };

            // Act
            var token = await test.Service.GenerateJwtTokenAsync(userId, email, roles);

            // Assert
            Assert.NotNull(token);
            Assert.NotEmpty(token);

            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            Assert.Equal("AIHelpDesk.Tests", jwtToken.Issuer);
            Assert.Contains("AIHelpDesk.Tests", jwtToken.Audiences);
            Assert.Equal(userId.ToString(), jwtToken.Claims.First(c => c.Type == "nameid").Value);
            Assert.Equal(email, jwtToken.Claims.First(c => c.Type == "email").Value);
            Assert.Equal("Customer", jwtToken.Claims.First(c => c.Type == "role").Value);
            Assert.NotNull(jwtToken.Claims.FirstOrDefault(c => c.Type == "jti"));
        }

        [Fact]
        public async Task GenerateJwtTokenAsync_WithMultipleRoles_ShouldIncludeAllRoles()
        {
            // Arrange
            var test = await CreateTestContextAsync();
            await using var connection = test.Connection;
            await using var context = test.Context;
            var userId = Guid.NewGuid();
            var roles = new[] { "SupportAgent", "Admin" };

            // Act
            var token = await test.Service.GenerateJwtTokenAsync(userId, "agent@example.com", roles);

            // Assert
            var jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(token);
            var roleClaims = jwtToken.Claims.Where(c => c.Type == "role").Select(c => c.Value).ToList();

            Assert.Contains("SupportAgent", roleClaims);
            Assert.Contains("Admin", roleClaims);
            Assert.Equal(2, roleClaims.Count);
        }

        [Fact]
        public async Task GenerateJwtTokenAsync_ShouldSetExpirationTime()
        {
            // Arrange
            var test = await CreateTestContextAsync();
            await using var connection = test.Connection;
            await using var context = test.Context;

            // Act
            var token = await test.Service.GenerateJwtTokenAsync(Guid.NewGuid(), "john@example.com", new[] { "Customer" });

            // Assert
            var jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(token);

            Assert.True(jwtToken.ValidTo > DateTime.UtcNow);
            Assert.True(jwtToken.ValidTo <= DateTime.UtcNow.AddMinutes(31));
        }

        [Fact]
        public async Task GenerateRefreshTokenAsync_ShouldGenerateToken()
        {
            // Arrange
            var test = await CreateTestContextAsync();
            await using var connection = test.Connection;
            await using var context = test.Context;

            // Act
            var refreshToken = await test.Service.GenerateRefreshTokenAsync();

            // Assert
            Assert.NotNull(refreshToken);
            Assert.NotEmpty(refreshToken);
            Assert.True(refreshToken.Length > 50);
        }

        [Fact]
        public async Task GenerateRefreshTokenAsync_ShouldGenerateUniqueTokens()
        {
            // Arrange
            var test = await CreateTestContextAsync();
            await using var connection = test.Connection;
            await using var context = test.Context;

            // Act
            var firstToken = await test.Service.GenerateRefreshTokenAsync();
            var secondToken = await test.Service.GenerateRefreshTokenAsync();

            // Assert
            Assert.NotEqual(firstToken, secondToken);
        }

        [Fact]
        public async Task SaveRefreshTokenAsync_ShouldPersistRefreshToken()
        {
            // Arrange
            var test = await CreateTestContextAsync();
            await using var connection = test.Connection;
            await using var context = test.Context;
            var userId = Guid.NewGuid();
            var jwtTokenId = Guid.NewGuid().ToString();
            var refreshToken = "refresh-token-value";
            var expiresAt = DateTime.UtcNow.AddDays(7);

            // Act
            await test.Service.SaveRefreshTokenAsync(userId, jwtTokenId, refreshToken, expiresAt);

            // Assert
            var savedToken = await context.RefreshTokens.FirstOrDefaultAsync();

            Assert.NotNull(savedToken);
            Assert.Equal(userId, savedToken.UserId);
            Assert.Equal(jwtTokenId, savedToken.JwtTokenId);
            Assert.Equal(refreshToken, savedToken.RefreshTokenValue);
            Assert.Equal(expiresAt, savedToken.ExpiresAt);
            Assert.NotNull(savedToken.TokenFamilyId);
            Assert.Null(savedToken.RevokedAt);
        }

        [Fact]
        public async Task ValidateRefreshTokenAsync_WhenTokenIsValid_ShouldReturnValid()
        {
            // Arrange
            var test = await CreateTestContextAsync();
            await using var connection = test.Connection;
            await using var context = test.Context;
            var userId = Guid.NewGuid();
            var jwtTokenId = Guid.NewGuid().ToString();
            var refreshToken = "valid-refresh-token";
            var tokenFamilyId = Guid.NewGuid().ToString();
            var expiresAt = DateTime.UtcNow.AddDays(7);

            await test.Service.SaveRefreshTokenAsync(userId, jwtTokenId, refreshToken, expiresAt, tokenFamilyId);

            // Act
            var result = await test.Service.ValidateRefreshTokenAsync(refreshToken);

            // Assert
            Assert.True(result.IsValid);
            Assert.Equal(userId.ToString(), result.UserId);
            Assert.Equal(tokenFamilyId, result.TokenFamilyId);
            Assert.False(result.IsReused);
        }

        [Fact]
        public async Task ValidateRefreshTokenAsync_WhenTokenDoesNotExist_ShouldReturnInvalid()
        {
            // Arrange
            var test = await CreateTestContextAsync();
            await using var connection = test.Connection;
            await using var context = test.Context;

            // Act
            var result = await test.Service.ValidateRefreshTokenAsync("missing-refresh-token");

            // Assert
            Assert.False(result.IsValid);
            Assert.Null(result.UserId);
            Assert.Null(result.TokenFamilyId);
            Assert.False(result.IsReused);
        }

        [Fact]
        public async Task ValidateRefreshTokenAsync_WhenTokenIsExpired_ShouldReturnInvalid()
        {
            // Arrange
            var test = await CreateTestContextAsync();
            await using var connection = test.Connection;
            await using var context = test.Context;
            var userId = Guid.NewGuid();
            var jwtTokenId = Guid.NewGuid().ToString();
            var refreshToken = "expired-refresh-token";
            var tokenFamilyId = Guid.NewGuid().ToString();
            var expiresAt = DateTime.UtcNow.AddMinutes(-1);

            await test.Service.SaveRefreshTokenAsync(userId, jwtTokenId, refreshToken, expiresAt, tokenFamilyId);

            // Act
            var result = await test.Service.ValidateRefreshTokenAsync(refreshToken);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal(userId.ToString(), result.UserId);
            Assert.Equal(tokenFamilyId, result.TokenFamilyId);
            Assert.False(result.IsReused);
        }

        [Fact]
        public async Task RevokeRefreshTokenAsync_WhenTokenExists_ShouldRevokeToken()
        {
            // Arrange
            var test = await CreateTestContextAsync();
            await using var connection = test.Connection;
            await using var context = test.Context;
            var userId = Guid.NewGuid();
            var jwtTokenId = Guid.NewGuid().ToString();
            var refreshToken = "refresh-token";
            var expiresAt = DateTime.UtcNow.AddDays(7);

            await test.Service.SaveRefreshTokenAsync(userId, jwtTokenId, refreshToken, expiresAt);

            // Act
            var result = await test.Service.RevokeRefreshTokenAsync(refreshToken);

            // Assert
            Assert.True(result);

            var savedToken = await context.RefreshTokens.FirstAsync();
            Assert.NotNull(savedToken.RevokedAt);
        }

        [Fact]
        public async Task ValidateRefreshTokenAsync_WhenTokenIsRevoked_ShouldReturnReused()
        {
            // Arrange
            var test = await CreateTestContextAsync();
            await using var connection = test.Connection;
            await using var context = test.Context;
            var userId = Guid.NewGuid();
            var jwtTokenId = Guid.NewGuid().ToString();
            var refreshToken = "revoked-refresh-token";
            var tokenFamilyId = Guid.NewGuid().ToString();
            var expiresAt = DateTime.UtcNow.AddDays(7);

            await test.Service.SaveRefreshTokenAsync(userId, jwtTokenId, refreshToken, expiresAt, tokenFamilyId);
            await test.Service.RevokeRefreshTokenAsync(refreshToken);

            // Act
            var result = await test.Service.ValidateRefreshTokenAsync(refreshToken);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal(userId.ToString(), result.UserId);
            Assert.Equal(tokenFamilyId, result.TokenFamilyId);
            Assert.True(result.IsReused);
        }
    }
}
