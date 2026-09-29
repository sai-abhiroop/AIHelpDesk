using System;
using System.Collections.Generic;
using System.Text;

namespace AIHelpDesk.Application.Interfaces
{
    public interface ITokenService
    {
        Task<string> GenerateJwtTokenAsync(Guid userId, string email, IEnumerable<string> roles);
        Task<string> GenerateRefreshTokenAsync();
        Task SaveRefreshTokenAsync(Guid userId, string jwtTokenId, string refreshToken, DateTime expiresAt, string? tokenFamilyId = null);
        Task<bool> RevokeRefreshTokenAsync(string refreshToken);
        Task<(bool IsValid, string? UserId, string? TokenFamilyId, bool IsReused)> ValidateRefreshTokenAsync(string refreshToken);
    }
}
