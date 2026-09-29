namespace AIHelpDesk.Domain.Entities
{
    public class RefreshToken
    {
        public Guid Id { get; private set; }

        public Guid UserId { get; private set; }

        public string JwtTokenId { get; private set; } = string.Empty;

        public string RefreshTokenValue { get; private set; } = string.Empty;

        public string TokenFamilyId { get; private set; } = string.Empty;

        public DateTime ExpiresAt { get; private set; }

        public DateTime CreatedAt { get; private set; }

        public DateTime? RevokedAt { get; private set; }

        public RefreshToken(Guid userId,string jwtTokenId,string refreshTokenValue,string tokenFamilyId,DateTime expiresAt)
        {
            Id = Guid.NewGuid();
            UserId = userId;
            JwtTokenId = jwtTokenId;
            RefreshTokenValue = refreshTokenValue;
            TokenFamilyId = tokenFamilyId;
            ExpiresAt = expiresAt;
            CreatedAt = DateTime.UtcNow;
        }

        public void Revoke()
        {
            RevokedAt = DateTime.UtcNow;
        }
    }
}
