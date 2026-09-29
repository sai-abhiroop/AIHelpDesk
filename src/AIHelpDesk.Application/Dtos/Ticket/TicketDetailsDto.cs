using AIHelpDesk.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace AIHelpDesk.Application.Dtos.Ticket
{
    public class TicketDetailsDto
    {
        public Guid Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public TicketStatus Status { get; set; }

        public TicketPriority Priority { get; set; }

        public Guid CustomerId { get; set; }

        public Guid? AssignedAgentId { get; set; }

        public Guid? CategoryId { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public DateTime? ResolvedAt { get; set; }
    }
}
