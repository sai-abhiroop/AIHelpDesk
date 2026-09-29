using AIHelpDesk.Application.Dtos.AI;
using System;
using System.Collections.Generic;
using System.Text;

namespace AIHelpDesk.Application.Interfaces
{
    public interface IAiTicketAnalysisService
    {
        Task<AiTicketAnalysisDto> AnalyzeTicketAsync(string title, string description);

        Task<AiTicketAnalysisDto?> AnalyzeExistingTicketAsync(Guid ticketId, Guid userId, bool isAdmin);
    }
}
