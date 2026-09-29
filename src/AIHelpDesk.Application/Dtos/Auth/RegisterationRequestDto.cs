using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace AIHelpDesk.Application.Dtos.Auth
{
    public class RegisterationRequestDto
    {
        [Required][MaxLength(50)] 
        public required string FirstName { get; set; }

        [Required][MaxLength(50)] 
        public required string LastName { get; set; }

        [Required][EmailAddress] 
        public required string Email { get; set; }

        [Required] 
        public required string Password { get; set; }
    }
}
