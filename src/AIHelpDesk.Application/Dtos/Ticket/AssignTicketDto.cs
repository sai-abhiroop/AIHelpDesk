using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace AIHelpDesk.Application.Dtos.Ticket
{
    public class AssignTicketDto
    {
        [Required]
        public Guid AgentId { get; set; }
    }
}
