using System;
using System.Collections.Generic;
using System.Text;

namespace AIHelpDesk.Application.Dtos.Auth
{
    public class UserDto
    {
        public required Guid Id { get; set; }

        public required string FirstName { get; set; }

        public required string LastName { get; set; }

        public required string Email { get; set; }

        public required string Role { get; set; }
    }
}
