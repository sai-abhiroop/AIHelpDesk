using AIHelpDesk.Application.Dtos.Auth;
using System;
using System.Collections.Generic;
using System.Text;

namespace AIHelpDesk.Application.Interfaces
{
    public interface IAuthService
    {
        Task<UserDto?> RegisterAsync(RegisterationRequestDto registrationRequestDto);

        Task<TokenDto?> LoginAsync(LoginRequestDto loginRequestDto);

        Task<bool> IsEmailExistsAsync(string email);

        Task<TokenDto?> RefreshAccessTokenAsync(RefreshTokenRequestDto refreshTokenRequestDto);
    }
}
