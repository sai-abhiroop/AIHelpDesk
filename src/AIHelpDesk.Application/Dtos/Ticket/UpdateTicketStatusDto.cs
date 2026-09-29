using AIHelpDesk.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace AIHelpDesk.Application.Dtos.Ticket
{
    public class UpdateTicketStatusDto
    {
        [Required]
        public TicketStatus Status { get; set; }
    }
}
