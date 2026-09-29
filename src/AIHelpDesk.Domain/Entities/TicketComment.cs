using System;
using System.Collections.Generic;
using System.Text;

namespace AIHelpDesk.Domain.Entities
{
    public class TicketComment
    {
        public Guid Id { get; private set; }

        public Guid TicketId { get; private set; }

        public Guid UserId { get; private set; }

        public string Content { get; private set; } = string.Empty;

        public DateTime CreatedAt { get; private set; }

        public TicketComment(Guid ticketId, Guid userId, string content)
        {
            Id=Guid.NewGuid();

            TicketId = ticketId;
            UserId=userId;

            Content = content;

            CreatedAt = DateTime.UtcNow;
        }

    }
}
