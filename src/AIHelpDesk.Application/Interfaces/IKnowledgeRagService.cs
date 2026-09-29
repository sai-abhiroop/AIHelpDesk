using AIHelpDesk.Application.Dtos.KnowledgeBase;
using System;
using System.Collections.Generic;
using System.Text;

namespace AIHelpDesk.Application.Interfaces
{
    public interface IKnowledgeRagService
    {
        Task<RagResponseDto> AskAsync(string question);
    }
}
