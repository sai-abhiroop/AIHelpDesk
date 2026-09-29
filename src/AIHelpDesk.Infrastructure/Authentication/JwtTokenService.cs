using AIHelpDesk.Application.Interfaces;
using AIHelpDesk.Domain.Entities;
using AIHelpDesk.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace AIHelpDesk.Infrastructure.Authentication
{
    public class JwtTokenService : ITokenService
    {
        private readonly JwtSettings _jwtSettings;
        private readonly ApplicationDbContext _db;

        public JwtTokenService(IOptions<JwtSettings> jwtOptions, ApplicationDbContext db)
        {
            _jwtSettings = jwtOptions.Value;
            _db = db;
        }

        public Task<string> GenerateJwtTokenAsync(Guid userId, string email, IEnumerable<string> roles)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId.ToString()),
                new(ClaimTypes.Email, email),
                new( JwtRegisteredClaimNames.Jti,Guid.NewGuid().ToString())
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = Encoding.UTF8.GetBytes( _jwtSettings.SecretKey);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),

                Issuer = _jwtSettings.Issuer,

                Audience = _jwtSettings.Audience,

                Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),

                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key),SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();

            var token = tokenHandler.CreateToken(tokenDescriptor);

            return Task.FromResult(tokenHandler.WriteToken(token));
        }

        public async Task<string> GenerateRefreshTokenAsync()
        {
            var randomNumber = new byte[64];

            using var rng = RandomNumberGenerator.Create();

            rng.GetBytes(randomNumber);

            var refreshToken =Convert.ToBase64String(randomNumber);

            var exists = await _db.RefreshTokens.AnyAsync(r =>r.RefreshTokenValue == refreshToken);

            if (exists)
            {
                return await GenerateRefreshTokenAsync();
            }

            return refreshToken;
        }

        public async Task<bool> RevokeRefreshTokenAsync(string refreshToken)
        {
            var storedToken = await _db.RefreshTokens.FirstOrDefaultAsync(r => r.RefreshTokenValue == refreshToken);

            if (storedToken == null)
            {
                return false;
            }

            storedToken.Revoke();

            await _db.SaveChangesAsync();

            return true;
        }

        public async Task SaveRefreshTokenAsync(Guid userId, string jwtTokenId, string refreshToken, DateTime expiresAt, string? tokenFamilyId = null)
        {
            var familyId = tokenFamilyId ?? Guid.NewGuid().ToString();

            var refreshTokenEntity = new RefreshToken(userId,jwtTokenId,refreshToken,familyId,expiresAt);

            await _db.RefreshTokens.AddAsync(refreshTokenEntity);

            await _db.SaveChangesAsync();
        }

        public async Task<(bool IsValid, string? UserId, string? TokenFamilyId, bool IsReused)> ValidateRefreshTokenAsync(string refreshToken)
        {
            var storedToken = await _db.RefreshTokens.FirstOrDefaultAsync( r => r.RefreshTokenValue == refreshToken);

            if (storedToken == null)
            {
                return (false, null, null, false);
            }

            // Refresh token was already revoked.
            // Treat this as potential token reuse.
            if (storedToken.RevokedAt.HasValue)
            {
                var tokenFamily = await _db.RefreshTokens
                                           .Where(r =>r.TokenFamilyId == storedToken.TokenFamilyId && r.UserId == storedToken.UserId)
                                           .ToListAsync();

                foreach (var token in tokenFamily)
                {
                    token.Revoke();
                }

                await _db.SaveChangesAsync();

                return ( false,storedToken.UserId.ToString(),storedToken.TokenFamilyId,true);
            }

            if (storedToken.ExpiresAt <= DateTime.UtcNow)
            {
                return (false,storedToken.UserId.ToString(),storedToken.TokenFamilyId,false);
            }

            return (true,storedToken.UserId.ToString(),storedToken.TokenFamilyId,false);
        }
    }
}
