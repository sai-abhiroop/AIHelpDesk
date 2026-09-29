using System;
using System.Collections.Generic;
using System.Text;

namespace AIHelpDesk.Application.Dtos.TicketComment
{
    public class TicketCommentDto
    {
        public Guid Id { get; set; }
        public Guid TicketId { get; set; }
        public Guid UserId { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
