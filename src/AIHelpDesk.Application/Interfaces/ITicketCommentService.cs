using AIHelpDesk.Application.Dtos.TicketComment;
using System;
using System.Collections.Generic;
using System.Text;

namespace AIHelpDesk.Application.Interfaces
{
    public interface ITicketCommentService
    {
        Task<TicketCommentDto?> AddCommentAsync(Guid ticketId,Guid userId,bool isAdmin,CreateTicketCommentDto dto);

        Task<IEnumerable<TicketCommentDto>> GetCommentsAsync(Guid ticketId,Guid userId,bool isAdmin);
    }
}
