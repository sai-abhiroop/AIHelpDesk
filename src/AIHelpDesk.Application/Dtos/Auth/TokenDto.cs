using System;
using System.Collections.Generic;
using System.Text;

namespace AIHelpDesk.Application.Dtos.Auth
{
    public class TokenDto
    {
        public string? AccessToken { get; set; }

        public string? RefreshToken { get; set; }

        public DateTime ExpiresAt { get; set; }
    }
}
