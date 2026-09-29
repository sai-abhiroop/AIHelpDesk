using AIHelpDesk.Application.Dtos.Auth;
using AIHelpDesk.Application.Interfaces;
using AIHelpDesk.Infrastructure.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;

namespace AIHelpDesk.Infrastructure.Identity
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;
        private readonly JwtSettings _jwtSettings;
        private readonly ITokenService _tokenService;
        private readonly ILogger<AuthService> _logger;

        public AuthService(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole<Guid>> roleManager, IOptions<JwtSettings> jwtOptions, ITokenService tokenService, ILogger<AuthService> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _tokenService = tokenService;
            _jwtSettings = jwtOptions.Value;
            _logger = logger;
        }
        public async Task<bool> IsEmailExistsAsync(string email)
        {
            return await _userManager.FindByEmailAsync(email) != null;
        }

        public async Task<TokenDto?> LoginAsync(LoginRequestDto loginRequestDto)
        {
            var user = await _userManager.FindByEmailAsync(
           loginRequestDto.Email);

            if (user == null || !user.IsActive)
            {
                _logger.LogWarning( "Login failed for email {Email}: invalid credentials", loginRequestDto.Email);

                return null;
            }

            var passwordValid = await _userManager.CheckPasswordAsync(user,loginRequestDto.Password);

            if (!passwordValid)
            {
                _logger.LogWarning( "Login failed for email {Email}: invalid credentials",loginRequestDto.Email);

                return null;
            }

            var roles = await _userManager.GetRolesAsync(user);

            var accessToken = await _tokenService.GenerateJwtTokenAsync(user.Id,user.Email!,roles);

            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(accessToken);

            var jwtTokenId = jwtToken.Claims
                                     .First(claim => claim.Type == JwtRegisteredClaimNames.Jti)
                                     .Value;

            var refreshToken = await _tokenService.GenerateRefreshTokenAsync();

            var refreshTokenExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.RefreshTokenExpirationMinutes);

            await _tokenService.SaveRefreshTokenAsync(user.Id,jwtTokenId,refreshToken,refreshTokenExpiresAt);

            _logger.LogInformation("User logged in successfully. UserId: {UserId}",user.Id);

            return new TokenDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = jwtToken.ValidTo
            };
        }

        public async Task<TokenDto?> RefreshAccessTokenAsync(RefreshTokenRequestDto refreshTokenRequestDto)
        {
            if (string.IsNullOrWhiteSpace(refreshTokenRequestDto.RefreshToken))
            {
                return null;
            }

            var ( isValid,userId,tokenFamilyId,tokenReused) = await _tokenService.ValidateRefreshTokenAsync(refreshTokenRequestDto.RefreshToken);

            if (tokenReused)
            {
                _logger.LogWarning("Refresh token reuse detected for UserId {UserId}",userId);

                return null;
            }

            if (!isValid || string.IsNullOrEmpty(userId) ||  string.IsNullOrEmpty(tokenFamilyId))
            {
                return null;
            }

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null || !user.IsActive)
            {
                return null;
            }

            await _tokenService.RevokeRefreshTokenAsync( refreshTokenRequestDto.RefreshToken);

            var roles = await _userManager.GetRolesAsync(user);

            var accessToken = await _tokenService.GenerateJwtTokenAsync(user.Id,user.Email!, roles);

            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(accessToken);

            var jwtTokenId = jwtToken.Claims
                                     .First(claim => claim.Type == JwtRegisteredClaimNames.Jti)
                                     .Value;

            var newRefreshToken =await _tokenService.GenerateRefreshTokenAsync();

            var refreshTokenExpiresAt =DateTime.UtcNow.AddMinutes(_jwtSettings.RefreshTokenExpirationMinutes);

            await _tokenService.SaveRefreshTokenAsync(user.Id, jwtTokenId, newRefreshToken,refreshTokenExpiresAt, tokenFamilyId);

            return new TokenDto
            {
                AccessToken = accessToken,
                RefreshToken = newRefreshToken,
                ExpiresAt = jwtToken.ValidTo
            };
        }

        public async Task<UserDto?> RegisterAsync(RegisterationRequestDto registrationRequestDto)
        {
            if (await IsEmailExistsAsync(registrationRequestDto.Email))
            {
                throw new InvalidOperationException($"A user with email '{registrationRequestDto.Email}' already exists.");
            }

            var user = new ApplicationUser
            {
                FirstName = registrationRequestDto.FirstName,
                LastName = registrationRequestDto.LastName,
                Email = registrationRequestDto.Email,
                UserName = registrationRequestDto.Email,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user,registrationRequestDto.Password);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ",result.Errors.Select(error => error.Description));

                throw new ArgumentException($"Registration failed: {errors}");
            }

            const string defaultRole = "Customer";

            if (!await _roleManager.RoleExistsAsync(defaultRole))
            {
                var roleResult = await _roleManager.CreateAsync(new IdentityRole<Guid>(defaultRole));

                if (!roleResult.Succeeded)
                {
                    var errors = string.Join(", ",roleResult.Errors.Select(error => error.Description));

                    throw new InvalidOperationException($"Role creation failed: {errors}");
                }
            }

            var role = await _userManager.AddToRoleAsync(user, defaultRole);

            if (!role.Succeeded)
            {
                var errors = string.Join(", ", role.Errors.Select(error => error.Description));

                throw new InvalidOperationException($"Failed to assign default role: {errors}");
            }

            _logger.LogInformation("User registered successfully. UserId: {UserId}, Role: {Role}",user.Id,defaultRole);

            return new UserDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email!,
                Role = defaultRole
            };
        }
    }
}
