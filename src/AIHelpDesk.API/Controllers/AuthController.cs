using AIHelpDesk.Application.Dtos.Auth;
using AIHelpDesk.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AIHelpDesk.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<ActionResult<UserDto>> Register(RegisterationRequestDto registrationRequestDto)
        {
            var result = await _authService.RegisterAsync(registrationRequestDto);

            return Ok(result);
        }

        [HttpPost("login")]
        public async Task<ActionResult<TokenDto>> Login(LoginRequestDto loginRequestDto)
        {
            var result = await _authService.LoginAsync(loginRequestDto);

            if (result == null)
            {
                return Unauthorized("Invalid email or password.");
            }

            return Ok(result);
        }

        [HttpPost("refresh")]
        public async Task<ActionResult<TokenDto>> Refresh( RefreshTokenRequestDto refreshTokenRequestDto)
        {
            var result = await _authService.RefreshAccessTokenAsync( refreshTokenRequestDto);

            if (result == null)
            {
                return Unauthorized("Invalid or expired refresh token.");
            }

            return Ok(result);
        }
    }
}
