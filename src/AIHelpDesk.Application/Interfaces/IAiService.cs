using System;
using System.Collections.Generic;
using System.Text;

namespace AIHelpDesk.Application.Interfaces
{
    public interface IAiService
    {
        Task<string> ClassifyTicketAsync(string title, string description);
        Task<string> SuggestPriorityAsync(string title, string description);
        Task<string> SummarizeTicketAsync(string title, string description);
        Task<string> GenerateSuggestedResponseAsync(string title, string description);
        Task<string> GenerateKnowledgeBaseAnswerAsync(string question, string context);
    }
}
